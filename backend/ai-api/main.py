from fastapi import FastAPI

app = FastAPI(
    title="OptiLifts AI Engine",
    description="Python microservice for predictive modeling and LLM routing.",
)


@app.get("/health")
def health_check():
    return {
        "status": "alive",
        "message": "AI Engine skeleton is ready for future development",
    }
