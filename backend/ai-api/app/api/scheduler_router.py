from fastapi import APIRouter
from app.models.reschedule import RescheduleRequest, RescheduleResponse
import time 

from app.services.dynamic_scheduler_tier1_solver import attempt_tier_one

router = APIRouter(prefix="/ai-api", tags=["scheduler"])

@router.post("/reschedule", response_model=RescheduleResponse)
def reschedule_workouts(request: RescheduleRequest):
    start_time = time.time()
    exec_time = int((time.time() - start_time)*1000)

    # setting up initial scafolding for layer 1
    tier1_response = attempt_tier_one(request, exec_time)

    if (tier1_response is not None):
        tier1_response.execution_time_ms = int((time.time() - start_time) * 1000)
        return tier1_response

    

    return RescheduleResponse(
        user_id = request.user_id,
        execution_tier = "tier1",
        execution_time_ms = exec_time,
        rescheduled_entries= [],
        dropped_entries= []
    )