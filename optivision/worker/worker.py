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