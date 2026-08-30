from ortools.sat.python import cp_model
from typing import Optional
from datetime import timedelta
import time

from app.models.reschedule import RescheduleRequest, RescheduleResponse, RescheduledEntry

def attempt_tier_two(request: RescheduleRequest, start_time: float) -> Optional[RescheduleResponse]:
    model = cp_model.CpModel()
    solver = cp_model.CpSolver()

    solver.parameters.max_time_in_seconds = 2.0
    status = solver.Solve(model)


    if (status == cp_model.OTPTIMAL or status == cp_model.FEASIBLE):
        # cool resposne and stuff
        pass
    else: 
        return None

    return RescheduleResponse(
        user_id = request.user_id,
        execution_tier = "Tier2_CPSAT",
        execution_time_ms = int((time.time() - start_time) * 1000),
        rescheduled_entries= [],
        dropped_entries = []
    )

    