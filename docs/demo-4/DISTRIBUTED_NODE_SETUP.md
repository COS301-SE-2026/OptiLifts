# OptiVision Distributed Computing Setup Guide & Node Operations Manual  

## Table of Contents
1. [Overview & Purpose](#1-overview--purpose)
2. [Access Control & Authorisation (Who is Allowed to Do It)](#2-access-control--authorisation-who-is-allowed-to-do-it)
3. [System Constraints](#3-system-constraints)
   - [3.1 Hardware & GPU Constraints](#31-hardware--gpu-constraints)
   - [3.2 Network & Firewall Constraints](#32-network--firewall-constraints)
   - [3.3 Security & Secret Constraints](#33-security--secret-constraints)
   - [3.4 Operating System & Runtime Constraints](#34-operating-system--runtime-constraints)
4. [Distributed Architecture & Node Lifecycle](#4-distributed-architecture--node-lifecycle)
5. [Prerequisites](#5-prerequisites)
6. [Step-by-Step Setup Guide](#6-step-by-step-setup-guide)
   - [Option A: Production Setup (Docker Compose with GPU Passthrough)](#option-a-production-setup-docker-compose-with-gpu-passthrough)
   - [Option B: Development / Local Bare-Metal Setup](#option-b-development--local-bare-metal-setup)
7. [Environment Variable Configuration](#7-environment-variable-configuration)
8. [Verification & Health Checks](#8-verification--health-checks)
9. [Troubleshooting & FAQ](#9-troubleshooting--faq)

---

## 1. Overview & Purpose

The **OptiVision Distributed Processing Cluster** is responsible for executing heavy deep-learning biomechanical form analysis (pose estimation landmark processing and sliding-window 1D-CNN inference) for core compound lifts (Squat, Bench Press, and Deadlift).

To prevent computationally expensive GPU workloads from blocking or exhausting the core web API server, OptiLifts offloads analysis requests using an asynchronous, decoupled queue architecture. Independent, containerised worker nodes discover their local compute hardware (CUDA GPUs or multi-core CPUs), poll messages from an Azure Service Bus queue via AMQP, fetch 3D skeletal landmark payloads from Azure Blob Storage, execute sliding-window neural network inference, and post the classification results back to the core API webhook.

This manual provides the official operational guidelines, constraints, security parameters, and step-by-step installation instructions for provisioning, configuring, and maintaining worker nodes across cloud virtual machines, on-premises servers, or local developer workstations.

---

## 2. Access Control & Authorisation (Who is Allowed to Do It)

Operating in a distributed computing environment requires strict authorization boundaries to protect proprietary neural network models, cloud credentials, and user exercise data.

### 2.1 Role-Based Authorisation Matrix

| Role | Allowed Actions | Restrictions |
| :--- | :--- | :--- |
| **DevOps / Infrastructure Admin (Alex Lange & Core Team)** | • Provision new worker nodes in production.<br>• Generate, rotate, and manage `NODE_SECRET` keys.<br>• Access Azure Service Bus connection strings and Azure Blob Storage keys.<br>• Update production neural network model weights (`/models/*.pt`). | Full administrative clearance required. Keys must never be committed to public repositories. |
| **Node Operators / Cluster Maintainers** | • Deploy, restart, and monitor containerised worker node instances.<br>• Inspect runtime telemetry, hardware metrics, and heartbeat status.<br>• Perform routine host OS and NVIDIA driver maintenance. | Granted access only to node host machines and `.env` node configurations. No direct database access. |
| **Core API Services** | • Receive heartbeats and worker results via authenticated internal endpoints.<br>• Enqueue vision jobs onto the Service Bus queue. | Authenticated via `X-Node-Secret` shared authorisation header. |
| **End Users / Athletes** | • Submit videos or landmarks for form analysis via client UI.<br>• Query the status and feedback of their own submitted jobs. | **Strictly Prohibited** from accessing, provisioning, or inspecting worker nodes, Service Bus queues, or blob storage containers. |

### 2.2 Provisioning Policy
* **Authorised Operators Only:** Only designated project engineers (Alex Lange and authorised Capstone administrators) are permitted to provision nodes connecting to the live production Service Bus queue (`form-analysis-jobs`).
* **Vetting New Hosts:** Before any remote machine or on-premise workstation is joined to the distributed cluster, its host identity, network IP, and physical environment must be verified by the infrastructure lead.
* **Revocation & Decommissioning:** When a worker node is permanently decommissioned, its assigned `NODE_SECRET` must be cycled in the core API environment to prevent unauthorised worker heartbeats.

---

## 3. System Constraints

### 3.1 Hardware & GPU Constraints
* **Recommended Compute Engine:** NVIDIA GPU with CUDA Compute Capability 7.0+ (Turing, Ampere, Ada Lovelace, Hopper, or newer).
* **VRAM Requirements:**
  * Minimum: 2.0 GB VRAM.
  * Recommended: 4.0 GB+ VRAM (for batch inference and concurrent frame window passes).
* **CPU Fallback Mode:**
  * If no NVIDIA GPU or CUDA-compatible driver is detected, the worker automatically falls back to PyTorch CPU execution (`torch.device("cpu")`).
  * *Constraint:* CPU inference increases processing latency from ~150ms per rep to ~1.2–2.5s per rep. Nodes operating in CPU fallback mode must be monitored to ensure queue backlog does not accumulate during peak hours.
* **System RAM:** Minimum 4 GB RAM (8 GB recommended for host operating system stability).
* **Storage:** Minimum 10 GB free disk space (to store the PyTorch CUDA base image, dependencies, and model weight binaries).

### 3.2 Network & Firewall Constraints
* **Outbound AMQP / WebSocket Access:**
  * Outbound access to Azure Service Bus over TCP port `5671` (AMQP over TLS) or TCP port `443` (AMQP over WebSockets).
* **Outbound HTTPS (TCP 443):**
  * Azure Blob Storage (`*.blob.core.windows.net`) to fetch landmark coordinate payloads.
  * Core API Server (`https://api.optilifts.app` or custom `CORE_API_URL`) to send 15-second heartbeat pings and post final anomaly webhooks.
* **Inbound Ports:**
  * **Zero inbound open ports required.** Worker nodes function entirely as outbound polling consumers. They do not expose open listening HTTP/TCP ports, significantly reducing the host attack surface.

### 3.3 Security & Secret Constraints
* **Claim-Check Isolation:** Worker nodes receive only a reference message containing the `jobId` and `blobUrl`. Raw patient/athlete credentials or profile databases are never exposed to worker nodes.
* **Shared Secret Authentication:** All HTTP calls from worker nodes to the Core API (`/api/internal/workers/heartbeat`, `/api/internal/workers/offline`, `/api/Vision/worker-result`) must supply the `X-Node-Secret` header matching the environment secret on the API server.
* **Container Hardening:** Production worker containers run as a non-root system user (`USER app`, UID/GID non-root) to prevent container breakout vulnerabilities.

### 3.4 Operating System & Runtime Constraints
* **Host Operating Systems:** Ubuntu Linux 22.04 LTS / 24.04 LTS (recommended), Debian 12, or Windows 11 with WSL2 (Ubuntu).
* **Python Runtime:** Python 3.10.x (PyTorch 2.2.0 compatibility).
* **CUDA Driver Version:** NVIDIA Driver 535.xx or higher (supporting CUDA 12.1+).

---

### Node Lifecycle States
1. **Initialising:** The node executes `detect_hardware()`, scans for CUDA devices, constructs a unique host node ID (e.g., `node-workstation-rtx3080-a1b2c3`), and initializes weights from `/models`.
2. **Idle:** The node starts the background heartbeat thread (sending status `idle` every 15 seconds) and opens an AMQP receiver on the Service Bus queue.
3. **Busy:** The node locks a message, sets status to `busy`, streams landmark coordinates from Blob Storage, executes sliding-window inference, posts results back to the API callback, and calls `complete_message()`.
4. **Offline / Shutdown:** Upon catching `SIGINT` or `SIGTERM`, the signal handler sets status to `offline`, halts the heartbeat loop, notifies `/api/internal/workers/offline`, and cleanly closes the Service Bus connection.

---

## 5. Prerequisites

Before installing a worker node, ensure the host machine has the following dependencies installed:

### 5.1 Host System Dependencies
```bash
# 1. Update package lists
sudo apt-get update && sudo apt-get install -y curl git build-essential

# 2. Verify NVIDIA Driver (GPU nodes only)
nvidia-smi
```
*Expected output: NVIDIA-SMI table showing Driver Version 535+ and CUDA Version 12.1+.*

### 5.2 Docker & NVIDIA Container Toolkit (Recommended Method)
```bash
# Install Docker Engine
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install NVIDIA Container Toolkit (enables Docker GPU passthrough)
curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg
curl -s -L https://nvidia.github.io/libnvidia-container/stable/deb/nvidia-container-toolkit.list | \
  sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \
  sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list

sudo apt-get update
sudo apt-get install -y nvidia-container-toolkit
sudo nvidia-ctk runtime configure --runtime=docker
sudo systemctl restart docker
```

---

## 6. Step-by-Step Setup Guide

### Option A: Production Setup (Docker Compose with GPU Passthrough)

This is the official, containerised deployment method used in production environments.

#### Step 1: Clone the Repository
```bash
git clone https://github.com/COS301-SE-2026/OptiLifts.git
cd OptiLifts
```

#### Step 2: Configure Environment Variables
Create or verify the `.env` file in the project root:
```bash
cp .env.example .env
nano .env
```
*(Ensure all required Service Bus, Storage, and Secret keys are populated as detailed in Section 7).*

#### Step 3: Verify Neural Network Weight Binaries
Ensure the pre-trained PyTorch model weights are present in the models directory:
```bash
ls -lh optivision/worker/models/
```
*Expected files:*
* `squat_side.pt`
* `bench_press_side.pt`
* `deadlift_side.pt`

#### Step 4: Build and Launch the Worker Node
```bash
cd optivision
docker compose -f docker-compose.worker.yml up -d --build
```

#### Step 5: Tail Container Logs to Verify Initialization
```bash
docker logs -f optivision-worker-node
```
You should see the initialisation banner:
```
===================================================================
OptiVision Distributed Node Initialising
===================================================================
Machine Name: optivision-compute-01
Node ID: node-optivision-compute-01-rtx-4090-f8a2d1
Compute Engine: CUDA:0 (NVIDIA GeForce RTX 4090 - 24.0 GB VRAM)
Weights Dir: /app/training/weights
Service Bus: form-analysis-jobs
===================================================================
[AMQP Worker] Node connected. Awaiting messages...
```

---

### Option B: Development / Local Bare-Metal Setup

This setup is ideal for local testing, development, and machines without Docker support.

#### Step 1: Set Up Python 3.10 Virtual Environment
```bash
cd OptiLifts/optivision/worker
python3.10 -m venv venv
source venv/bin/activate
```

#### Step 2: Install PyTorch with CUDA Support
```bash
# For CUDA 12.1 acceleration:
pip install --upgrade pip
pip install torch torchvision --index-url https://download.pytorch.org/whl/cu121

# For CPU-only environments:
# pip install torch torchvision --index-url https://download.pytorch.org/whl/cpu
```

#### Step 3: Install Worker Dependencies
```bash
pip install -r requirements.txt
```

#### Step 4: Set Environment Configuration
Export the required environment variables or provide a `.env` file in the working directory:
```bash
export SERVICEBUS_CONNECTION_STRING="<Your Azure Service Bus Connection String>"
export SERVICEBUS_QUEUE_NAME="form-analysis-jobs"
export AZURE_STORAGE_CONNECTION_STRING="<Your Azure Storage Connection String>"
export CORE_API_URL="https://api.optilifts.app"
export NODE_SECRET="<Your Authorised Node Secret>"
export HEARTBEAT_INTERVAL_SECONDS="15"
```

#### Step 5: Execute the Worker
```bash
python worker.py
```

---

## 7. Environment Variable Configuration

Every worker node relies on the following environment variables. Misconfigurations will cause immediate shutdown or authorisation denial.

| Variable Name | Required | Default Value | Description |
| :--- | :---: | :--- | :--- |
| `SERVICEBUS_CONNECTION_STRING` | **Yes** | *None* | Primary connection string for the Azure Service Bus namespace. |
| `SERVICEBUS_QUEUE_NAME` | No | `form-analysis-jobs` | Target AMQP queue name where vision tasks are dispatched. |
| `AZURE_STORAGE_CONNECTION_STRING` | **Yes** | *None* | Storage account connection string to download landmark payloads. |
| `CORE_API_URL` | No | `https://api.optilifts.app` | Base URL of the Core .NET API for heartbeats and callbacks. |
| `NODE_SECRET` | **Yes** | *Empty String* | Shared authorisation secret passed in the `X-Node-Secret` header. |
| `HEARTBEAT_INTERVAL_SECONDS` | No | `15` | Frequency in seconds between worker status pings. |

---

## 8. Verification & Health Checks

Once a node is running, execute the following steps to verify operational health:

### 8.1 Verification of Live Heartbeat
Check the Core API internal worker registry (authorised administrators only):
```bash
curl -X GET "https://api.optilifts.app/api/internal/workers" \
  -H "X-Node-Secret: <NODE_SECRET>"
```
*Expected response confirms the node is listed with status `"idle"` and matches its detected `nodeId` and `device` name.*

### 8.2 End-to-End Test Job Execution
1. Navigate to the OptiLifts Web Application (`/form-check`).
2. Select **Squat**, upload a side-profile workout video or test coordinate JSON file, and click **Analyse Form**.
3. Inspect worker logs:
```
[AMQP Worker] Received message for Job: job_7f9b8c1d
[AMQP Worker] State changed to 'busy'. Downloading landmarks from Blob Storage...
[AMQP Worker] Executing sliding window inference (Model: squat_side.pt)...
[AMQP Worker] Detected 2 anomalies: [Insufficient squat depth, Knees caving inward]
[AMQP Worker] Reporting results to Core API webhook...
[AMQP Worker] Completed message successfully. State restored to 'idle'.
```
4. Verify on the client UI that the movement score, coaching tips, and anomaly breakdown appear within 2–5 seconds.

---

## 9. Troubleshooting & FAQ

### Issue 1: `Compute Engine: PyTorch CPU` instead of CUDA
* **Cause:** NVIDIA Container Toolkit is not configured, or host NVIDIA drivers are missing.
* **Resolution:**
  1. Test host GPU access: `nvidia-smi`.
  2. Test Docker GPU access: `docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi`.
  3. Ensure `optivision/docker-compose.worker.yml` contains the `deploy.resources.reservations.devices` block with capability `[gpu]`.

### Issue 2: Node exits immediately with exit code 1
* **Cause:** Missing mandatory connection strings in `.env`.
* **Resolution:** Check `docker logs optivision-worker-node`. If `SERVICEBUS_CONNECTION_STRING` or `AZURE_STORAGE_CONNECTION_STRING` is missing, `validate_config()` aborts execution. Ensure `.env` is mounted correctly.

### Issue 3: `401 Unauthorised` during Heartbeat or Webhook callback
* **Cause:** `NODE_SECRET` mismatch between the worker node and the Core API server.
* **Resolution:** Verify that `NODE_SECRET` in the worker `.env` matches the `NODE_SECRET` environment variable configured in the Azure Container App / Core API service settings.

### Issue 4: Message stuck in queue / Repeated re-queuing
* **Cause:** Worker failed to process landmark coordinates or encountered a corrupted JSON payload, triggering `abandon_message()`.
* **Resolution:** Verify Blob Storage permissions and ensure client uploads match the schema expected by `sliding_window.py` (sequential landmark arrays with `x`, `y`, `z` coordinates).


