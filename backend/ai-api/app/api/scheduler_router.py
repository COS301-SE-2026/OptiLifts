from fastapi import APIRouter
from app.models.reschedule import RescheduleRequest, RescheduleResponse
import time 

from app.services.dynamic_scheduler_tier1_solver import attempt_tier_one
from app.services.dynamic_scheduler_tier2_solver import attempt_tier_two

router = APIRouter(prefix="/ai-api", tags=["scheduler"])

@router.post("/reschedule", response_model=RescheduleResponse)
def reschedule_workouts(request: RescheduleRequest):
    start_time = time.time()
    exec_time = int((time.time() - start_time)*1000)

    #try tier 1
    tier1_response = attempt_tier_one(request, exec_time)

    if (tier1_response is not None):
        tier1_response.execution_time_ms = int((time.time() - start_time) * 1000)
        return tier1_response

    #try tier 2
    tier2_response = attempt_tier_two(request, start_time)

    if (tier2_response is not None):
        tier2_response.execution_time_ms = int((time.time() - start_time) * 1000)
        return tier2_response

    
    # Both tiers failed :(
    # placeholder for what to do if it's infeasible to reschedule all
    return RescheduleResponse(
        user_id = request.user_id,
        execution_tier = "Failed",
        execution_time_ms = int((time.time() - start_time) * 1000),
        rescheduled_entries = [],
        dropped_entries = request.entries
    )