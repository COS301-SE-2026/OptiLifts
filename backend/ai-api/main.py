# EXAMPLE FILE - shows how to document API endpoints for Swagger
# Copy this pattern when adding real routes

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

tags_metadata = [
    {"name": "health", "description": "Service health checks."},
    {"name": "workouts", "description": "AI-powered workout recommendations."},
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

# --- Models ---


class RecommendRequest(BaseModel):
    user_id: str
    primary_muscles: list[str]
    available_equipment: list[str]


class ExerciseRecommendation(BaseModel):
    name: str
    muscle_group: str
    equipment: str
    estimated_sets: int


class RecommendResponse(BaseModel):
    user_id: str
    recommendations: list[ExerciseRecommendation]


# --- Routes ---


@app.get("/health", tags=["health"], summary="Health check")
def health_check():
    """Returns the current status of the AI Engine service."""
    return {"status": "alive", "message": "AI Engine is ready."}


@app.post(
    "/recommendations",
    tags=["workouts"],
    summary="Get AI exercise recommendations",
    response_model=RecommendResponse,
    responses={
        200: {"description": "Recommendations generated successfully."},
        400: {"description": "Invalid request body."},
        404: {"description": "User not found."},
    },
)
def get_recommendations(request: RecommendRequest):
    """
    Returns a list of AI-generated exercise recommendations based on the
    user's target muscle groups and available equipment.

    - **user_id**: the user to generate recommendations for
    - **primary_muscles**: muscle groups to target (e.g. Biceps, Chest)
    - **available_equipment**: equipment available (e.g. Dumbbell, Barbell)
    """
    #plug in real ML/LLM logic
    if not request.primary_muscles:
        raise HTTPException(status_code=400, detail="primary_muscles cannot be empty.")

    return RecommendResponse(
        user_id=request.user_id,
        recommendations=[
            ExerciseRecommendation(
                name="Bicep Curl",
                muscle_group="Biceps",
                equipment="Dumbbell",
                estimated_sets=3,
            )
        ],
    )
