from fastapi import FastAPI

tags_metadata = [
    {
        "name": "health",
        "description": "Service health checks.",
    },
]

app = FastAPI(
    title="OptiLifts AI Engine",
    description="Python microservice for predictive modelling and LLM routing.",
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
