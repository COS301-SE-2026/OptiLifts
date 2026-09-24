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