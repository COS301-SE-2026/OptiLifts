# EXAMPLE FILE - shows how to document API endpoints for Swagger
# Copy this pattern when adding real routes

import os
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from app.api.scheduler_router import router as scheduler_router

tags_metadata = [
    {"name": "health", "description": "Service health checks."},
    {"name": "scheduler", "description": "AI-powered workout rescheduling."},
]

is_dev = os.getenv(
    "ENVIRONMENT", os.getenv("APP_ENV", os.getenv("PYTHON_ENV", "development"))
).lower() in (
    "dev",
    "development",
)

app = FastAPI(
    title="OptiLifts AI Engine",
    description="Python service for predictive modelling and AI scheduling.",
    version="0.1.0",
    contact={"name": "OptiLifts Team"},
    openapi_tags=tags_metadata,
    docs_url="/docs" if is_dev else None,
    redoc_url="/redoc" if is_dev else None,
    openapi_url="/openapi.json" if is_dev else None,
)


@app.get("/health", tags=["health"], summary="Health check")
def health_check():
    """Returns the current status of the AI Engine service."""
    return {"status": "alive", "message": "AI Engine is ready."}


app.include_router(scheduler_router)
