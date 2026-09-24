import json
import logging
import os
import platform
import signal
import socket
import sys
import threading
import time
import uuid
from pathlib import Path
from typing import Any, Dict, List, Optional
from urllib.parse import urlparse
import numpy as np
import requests
import torch
from azure.servicebus import ServiceBusClient, ServiceBusReceiver, ServiceBusReceivedMessage
from azure.storage.blob import BlobClient
from dotenv import load_dotenv

load_dotenv()
logging.basicConfig(
    level=logging.INFO,
    format="[%(asctime)s] [%(levelname)s] [AMQP Worker] %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
logger = logging.getLogger("OptiVisionWorker")

#config
SERVICEBUS_CONN_STR = os.getenv("SERVICEBUS_CONNECTION_STRING")
QUEUE_NAME = os.getenv("SERVICEBUS_QUEUE_NAME", "form-analysis-jobs")
STORAGE_CONN_STR = os.getenv("AZURE_STORAGE_CONNECTION_STRING")
CORE_API_URL = os.getenv("CORE_API_URL", "https://api.optilifts.app").rstrip("/")
INTERNAL_SECRET = os.getenv("INTERNAL_WORKER_SECRET", "")
HEARTBEAT_INTERVAL_SECONDS = int(os.getenv("HEARTBEAT_INTERVAL_SECONDS", "15"))
APPLICATION_JSON = "application/json"

#sliding window params
WEIGHTS_DIR = Path(__file__).resolve().parent.parent / "training" / "weights"
WINDOW_SIZE = 90
STRIDE = 30
THRESHOLD = 0.80

LABELS = {
    "squat": ["shallow_depth", "excessive_forward_lean", "heels_raised"],
    "bench_press": ["glutes_raised", "excessive_elbow_flare", "no_chest_touch", "incorrect_bar_path", "bad_arch"],
    "deadlift": ["lumbar_flexion", "hips_early_rise", "bar_drifting", "knees_forward", "shallow_depth"]
}

EXERCISE_ALIASES = {
    "bench": "bench_press",
    "benchpress": "bench_press",
    "bench_press": "bench_press",
    "squat": "squat",
    "squats": "squat",
    "deadlift": "deadlift",
    "deadlifts": "deadlift",
}

#states
RUNNING = True
CURRENT_STATE = "idle"
CURRENT_JOB_ID: Optional[str] = None
HEARTBEAT_STOP_EVENT = threading.Event()
LOADED_MODELS: Dict[str, Any] = {}
DEVICE: torch.device = torch.device("cpu")


def detect_hardware():
    global DEVICE
    hostname = socket.gethostname()
    os_info = f"{platform.system()} {platform.release()}"
    device_name = "CPU Only"
    total_memory_gb = 0.0

    try:
        if torch.cuda.is_available():
            DEVICE = torch.device("cuda:0")
            device_name = torch.cuda.get_device_name(0)
            mem_bytes = torch.cuda.get_device_properties(0).total_memory
            total_memory_gb = round(mem_bytes / (1024**3), 1)
            compute_engine = f"CUDA:0 ({device_name} - {total_memory_gb} GB VRAM)"
        else:
            DEVICE = torch.device("cpu")
            compute_engine = f"PyTorch CPU ({os_info})"
    except Exception as exc:
        DEVICE = torch.device("cpu")
        compute_engine = f"CPU Fallback ({exc})"

    short_id = uuid.uuid4().hex[:6]
    clean_dev = device_name.lower().replace(" ", "-").replace("nvidia-", "")[:16]
    node_id = f"node-{hostname}-{clean_dev}-{short_id}"

    return {
        "node_id": node_id,
        "hostname": hostname,
        "os": os_info,
        "device_name": device_name,
        "compute_engine": compute_engine,
        "vram_gb": total_memory_gb,
    }

NODE_INFO = detect_hardware()

def get_model(exercise: str):
    normalised = EXERCISE_ALIASES.get(exercise.lower(), exercise.lower())
    if normalised in LOADED_MODELS:
        return normalised, LOADED_MODELS[normalised]

    model_file = WEIGHTS_DIR / f"{normalised}_side.pt"
    if not model_file.exists():
        raise FileNotFoundError(f"Model weights not found for '{normalised}' at {model_file}")

    logger.info("Loading TorchScript model: %s onto %s...", model_file.name, DEVICE)
    model = torch.jit.load(str(model_file), map_location=DEVICE)
    model.eval()
    LOADED_MODELS[normalised] = model
    return normalised, model


def send_heartbeat_ping(status: str, job_id: Optional[str] = None) -> bool:
    url = f"{CORE_API_URL}/api/internal/workers/heartbeat"
    headers = {"Content-Type": APPLICATION_JSON}
    if INTERNAL_SECRET:
        headers["X-Internal-Secret"] = INTERNAL_SECRET

    payload = {
        "workerId": NODE_INFO["node_id"],
        "hostname": NODE_INFO["hostname"],
        "device": NODE_INFO["compute_engine"],
        "status": status,
        "currentJobId": job_id,
        "timestamp": time.time(),
    }

    try:
        res = requests.post(url, json=payload, headers=headers, timeout=5)
        return res.status_code in (200, 201, 204)
    except requests.exceptions.RequestException:
        return False


def heartbeat_daemon():
    logger.info("Heartbeat daemon thread active (interval: %ds).", HEARTBEAT_INTERVAL_SECONDS)
    while not HEARTBEAT_STOP_EVENT.is_set():
        send_heartbeat_ping(CURRENT_STATE, CURRENT_JOB_ID)
        HEARTBEAT_STOP_EVENT.wait(HEARTBEAT_INTERVAL_SECONDS)


def notify_offline():
    url = f"{CORE_API_URL}/api/internal/workers/offline"
    headers = {"Content-Type": APPLICATION_JSON}
    if INTERNAL_SECRET:
        headers["X-Internal-Secret"] = INTERNAL_SECRET

    payload = {
        "workerId": NODE_INFO["node_id"],
        "status": "offline",
    }
    try:
        requests.post(url, json=payload, headers=headers, timeout=3)
        logger.info("Deregistration sent: node marked offline in cluster.")
    except Exception:
        pass


def signal_handler(signum, frame):
    global RUNNING, CURRENT_STATE
    CURRENT_STATE = "offline"
    logger.info("\nShutdown signal received (%s). Stopping AMQP listener...", signum)
    RUNNING = False
    HEARTBEAT_STOP_EVENT.set()
    notify_offline()


signal.signal(signal.SIGINT, signal_handler)
signal.signal(signal.SIGTERM, signal_handler)


def validate_config():
    missing = []
    if not SERVICEBUS_CONN_STR:
        missing.append("SERVICEBUS_CONNECTION_STRING")
    if not STORAGE_CONN_STR:
        missing.append("AZURE_STORAGE_CONNECTION_STRING")
    if missing:
        logger.error("Missing required environment variables: %s", ", ".join(missing))
        sys.exit(1)