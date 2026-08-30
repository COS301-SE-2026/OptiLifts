from datetime import timedelta
from typing import Optional

from app.models.reschedule import RescheduleRequest, RescheduleResponse, RescheduledEntry

def attempt_tier_one(request: RescheduleRequest, execution_time_ms: int = 0) -> Optional[RescheduleResponse]:
    missed_workouts = [entry for entry in request.entries if entry.status == "Missed"]
    scheduled_workouts = [entry for entry in request.entries if entry.status == "Scheduled"]

    if len(missed_workouts) != 1: 
        return None #python is funky

    missed_workout = missed_workouts[0]

    curr_day = request.planning_window_start
    while (curr_day.date() <= request.planning_window_end.date()): 
        day_name = curr_day.strftime("%A")

        #rest day
        if day_name in request.preferences.fixed_rest_days:
            curr_day+= timedelta(days=1) 
            continue

        #max workouts per day check
        count_w = 0
        for entry in scheduled_workouts: 
            if entry.scheduled_at.date() == curr_day.date(): 
                count_w+= 1

        if count_w >= request.preferences.max_workouts_per_day:
            curr_day += timedelta(days=1) 
            continue

        # good day
        new_datetime = curr_day.replace(
            hour=missed_workout.scheduled_at.hour,
            minute=missed_workout.scheduled_at.minute
        )

        rescheduled_entry = RescheduledEntry(
            entry_id = missed_workout.id,
            workout_id = missed_workout.workout_id,
            workout_name = missed_workout.workout_name,
            original_scheduled_at = missed_workout.scheduled_at,
            new_scheduled_at = new_datetime,
            action="Shifted"
        )

        return RescheduleResponse(
            user_id = request.user_id,
            execution_tier = "Tier1_FastPath",
            execution_time_ms = execution_time_ms,
            rescheduled_entries = [rescheduled_entry],
            dropped_entries = []
        )

    # unsuccesful, no valid day
    return None

            