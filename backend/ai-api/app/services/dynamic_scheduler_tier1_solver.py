from datetime import timedelta
from typing import Optional

from app.models.reschedule import (
    RescheduleRequest,
    RescheduleResponse,
    RescheduledEntry,
)


def _has_max_capacity(curr_date, scheduled_workouts, max_workouts: int) -> bool:
    count = sum(1 for entry in scheduled_workouts if entry.scheduled_at.date() == curr_date)
    return count >= max_workouts

def _has_muscle_conflict(curr_date, missed_workout, scheduled_workouts, min_days: float) -> bool:
    if min_days <= 0:
        return False
        
    first_muscles = set(missed_workout.primary_muscles)
    for entry in scheduled_workouts:
        if first_muscles.intersection(entry.primary_muscles):
            diff_days = abs((curr_date - entry.scheduled_at.date()).days)
            if diff_days < min_days:
                return True
    return False

def attempt_tier_one(
    request: RescheduleRequest, execution_time_ms: int = 0
) -> Optional[RescheduleResponse]:
    missed_workouts = [entry for entry in request.entries if entry.status == "Missed"]
    
    if len(missed_workouts) != 1:
        return None  # python is funky

    missed_workout = missed_workouts[0]
    scheduled_workouts = [
        entry for entry in request.entries if entry.status == "Scheduled"
    ]
    
    min_days = request.preferences.min_muscle_rest_hours / 24
    curr_day = request.planning_window_start
    end_date = request.planning_window_end.date()

    while curr_day.date() <= end_date:
        curr_date = curr_day.date()
        
        # rest day
        if curr_day.strftime("%A") in request.preferences.fixed_rest_days:
            curr_day += timedelta(days=1)
            continue

        # max workouts per day check
        if _has_max_capacity(curr_date, scheduled_workouts, request.preferences.max_workouts_per_day):
            curr_day += timedelta(days=1)
            continue

        # muscle conflict
        if _has_muscle_conflict(curr_date, missed_workout, scheduled_workouts, min_days):
            curr_day += timedelta(days=1)
            continue

        # good day
        new_datetime = curr_day.replace(
            hour=missed_workout.scheduled_at.hour,
            minute=missed_workout.scheduled_at.minute,
        )

        rescheduled_entry = RescheduledEntry(
            entry_id=missed_workout.id,
            workout_id=missed_workout.workout_id,
            workout_name=missed_workout.workout_name,
            original_scheduled_at=missed_workout.scheduled_at,
            new_scheduled_at=new_datetime,
            action="Shifted",
        )

        return RescheduleResponse(
            user_id=request.user_id,
            execution_tier="Tier1_FastPath",
            execution_time_ms=execution_time_ms,
            rescheduled_entries=[rescheduled_entry],
            dropped_entries=[],
        )

    # unsuccesful, no valid day
    return None
