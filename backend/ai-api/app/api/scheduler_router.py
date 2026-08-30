from fastapi import APIRouter
from app.models.reschedule import RescheduleRequest, RescheduleResponse
import time 

router = APIRouter(prefix="/ai-api", tags=["scheduler"])

@router.post("/reschedule", response_model=RescheduleResponse)
def reschedule_workouts(request: RescheduleRequest):
    start_time = time.time()

    # setting up initial scafolding for layer 1

    exec_time = int((time.time() - start_time)*1000)

    return RescheduleResponse(
        user_id = request.user_id,
        execution_tier = "tier1",
        execution_time_ms = exec_time,
        rescheduled_entries= [],
        dropped_entries= []
    )