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
from typing import Any, Dict, List, Optional
from urllib.parse import urlparse
import requests
import torch
from azure.servicebus import ServiceBusClient, ServiceBusReceiver, ServiceBusReceivedMessage
from azure.storage.blob import BlobClient
from dotenv import load_dotenv

from sliding_window import (
    WEIGHTS_DIR,
    set_device,
    run_sliding_window_inference,
)


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

#states
RUNNING = True
CURRENT_STATE = "idle"
CURRENT_JOB_ID: Optional[str] = None
HEARTBEAT_STOP_EVENT = threading.Event()
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

    set_device(DEVICE)
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
    logger.info("\nShutdown signal received (%s). Stopping AMQP listener.", signum)
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


def download_coordinates(blob_url_or_name: str) -> Dict[str, Any]:
    logger.info("Downloading coordinate payload from: %s", blob_url_or_name)

    if blob_url_or_name.startswith(("http://", "https://")):
        parsed = urlparse(blob_url_or_name)
        parts = parsed.path.lstrip("/").split("/", 1)
        if len(parts) == 2:
            container_name, blob_name = parts
            blob_client = BlobClient.from_connection_string(
                conn_str=STORAGE_CONN_STR,
                container_name=container_name,
                blob_name=blob_name,
            )
        else:
            blob_client = BlobClient.from_blob_url(blob_url=blob_url_or_name)
    else:
        blob_client = BlobClient.from_connection_string(
            conn_str=STORAGE_CONN_STR,
            container_name="optivision-payloads",
            blob_name=blob_url_or_name,
        )

    stream = blob_client.download_blob()
    content = stream.readall()
    return json.loads(content.decode("utf-8"))


def report_results_to_backend(job_id: str, anomalies: List[Dict[str, Any]]):
    webhook_url = f"{CORE_API_URL}/api/Vision/worker-result"
    headers = {
        "Content-Type": APPLICATION_JSON,
    }
    if INTERNAL_SECRET:
        headers["X-Internal-Secret"] = INTERNAL_SECRET

    payload = {
        "jobId": job_id,
        "success": True,
        "detected_anomalies": anomalies,
    }

    logger.info("Reporting results to Core API webhook (%s) for job %s.", webhook_url, job_id)
    response = requests.post(webhook_url, json=payload, headers=headers, timeout=30)
    response.raise_for_status()
    logger.info("Results successfully accepted by Core API (HTTP %d).", response.status_code)


def process_message(receiver: ServiceBusReceiver, message: ServiceBusReceivedMessage):
    global CURRENT_STATE, CURRENT_JOB_ID
    raw_body = str(message)
    logger.info("[JOB RECEIVED] Message: %s", raw_body)

    try:
        data = json.loads(raw_body)
        job_id = data.get("jobId") or data.get("job_id") or data.get("JobId")
        exercise = data.get("exercise") or data.get("exerciseType") or data.get("Exercise", "squat")
        blob_url = data.get("blobUrl") or data.get("blob_url") or data.get("BlobUrl")

        if not job_id or not blob_url:
            raise ValueError(f"Message missing required fields ('jobId', 'blobUrl'): {data}")

        CURRENT_STATE = "busy"
        CURRENT_JOB_ID = job_id
        logger.info("Starting processing for Job ID: %s (Exercise: %s)", job_id, exercise)
        coordinates = download_coordinates(blob_url)
        anomalies = run_sliding_window_inference(exercise, coordinates)
        report_results_to_backend(job_id, anomalies)
        receiver.complete_message(message)
        logger.info("Job %s completed successfully and acknowledged on Service Bus.", job_id)

    except Exception as exc:
        logger.exception("Failed to process message (Job error: %s). Abandoning for retry", exc)
        try:
            receiver.abandon_message(message)
        except Exception as abandon_err:
            logger.exception("Failed to abandon message on Service Bus: %s", abandon_err)
    finally:
        CURRENT_STATE = "idle"
        CURRENT_JOB_ID = None


def print_banner():
    print("=" * 67)
    print("OptiVision Distributed Node Initialising")
    print("=" * 67)
    print(f"Machine Name: {NODE_INFO['hostname']}")
    print(f"Node ID: {NODE_INFO['node_id']}")
    print(f"Compute Engine: {NODE_INFO['compute_engine']}")
    print(f"Weights Dir: {WEIGHTS_DIR}")
    print(f"Service Bus: {QUEUE_NAME}")
    print("=" * 67)


def check_model_weights():
    for ex in ("squat", "bench_press", "deadlift"):
        weights_file = WEIGHTS_DIR / f"{ex}_side.pt"
        status = "FOUND" if weights_file.exists() else "MISSING"
        logger.info("Model weights [%s]: %s (%s)", ex, status, weights_file.name)


def listen_for_jobs(receiver: ServiceBusReceiver):
    while RUNNING:
        messages = receiver.receive_messages(max_message_count=1, max_wait_time=10)
        for msg in messages:
            if not RUNNING:
                receiver.abandon_message(msg)
                return
            process_message(receiver, msg)


def main():
    validate_config()
    print_banner()
    check_model_weights()
    heartbeat_thread = threading.Thread(target=heartbeat_daemon, daemon=True)
    heartbeat_thread.start()
    while RUNNING:
        try:
            logger.info("Connecting to Azure Service Bus over AMQP 1.0")
            with ServiceBusClient.from_connection_string(
                conn_str=SERVICEBUS_CONN_STR,
                logging_enable=False,
            ) as sb_client:
                with sb_client.get_queue_receiver(
                    queue_name=QUEUE_NAME,
                    prefetch_count=1,
                    max_wait_time=10,
                ) as receiver:
                    logger.info("AMQP listener active. Worker Node ONLINE and waiting for jobs")
                    listen_for_jobs(receiver)
        except Exception as exc:
            if not RUNNING:
                break
            logger.exception("AMQP connection dropped: %s. Reconnecting in 5 seconds", exc)
            time.sleep(5)

    logger.info("OptiVision AMQP Worker Daemon shut down.")


if __name__ == "__main__":
    main()