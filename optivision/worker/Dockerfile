# OptiVision Distributed GPU Worker Node
# Base image with Python 3.10 and PyTorch with CUDA 12.1 support
FROM pytorch/pytorch:2.2.0-cuda12.1-cudnn8-runtime

ENV PYTHONUNBUFFERED=1 \
    DEBIAN_FRONTEND=noninteractive

WORKDIR /app

# Install system dependencies (curl for healthchecks)
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Install python requirements
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt \
    && addgroup --system app \
    && adduser --system --group app

# Copy worker application
COPY worker.py sliding_window.py ./

COPY models /app/training/weights

USER app

CMD ["python", "worker.py"]

