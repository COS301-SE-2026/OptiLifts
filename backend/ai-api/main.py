from fastapi import FastAPI
from app.api.scheduler_router import router as scheduler_router

tags_metadata = [
    {"name": "health", "description": "Service health checks."},
    {"name": "scheduler", "description": "AI-powered workout rescheduling."},
]
app = FastAPI(
    title="OptiLifts AI Engine",
    description="Python service for predictive modelling and AI scheduling.",
    version="0.1.0",
    contact={"name": "OptiLifts Team"},
    openapi_tags=tags_metadata,
    docs_url="/docs",
    redoc_url="/redoc",
)


@app.get("/health", tags=["health"], summary="Health check")
def health_check():
    """Returns the current status of the AI Engine service."""
    return {"status": "alive", "message": "AI Engine is ready."}


app.include_router(scheduler_router)
