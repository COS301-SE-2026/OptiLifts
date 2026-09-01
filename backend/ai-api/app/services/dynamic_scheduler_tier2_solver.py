from ortools.sat.python import cp_model
from typing import Optional, List
from datetime import timedelta
import time

from app.models.reschedule import (
    RescheduleRequest,
    RescheduleResponse,
    RescheduledEntry,
)


def _get_available_days(start_date, end_date) -> List:
    available_days = []
    curr_day = start_date
    while curr_day.date() <= end_date.date():
        available_days.append(curr_day)
        curr_day += timedelta(days=1)
    return available_days


def _apply_basic_constraints(
    model, schedule_vars, num_entries, num_days, available_days, preferences
):
    # constraints
    # every workout scheduled 0/1 times
    for workout in range(num_entries):
        model.Add(sum(schedule_vars[(workout, day)] for day in range(num_days)) <= 1)

    # rest days
    for day in range(num_days):
        day_name = available_days[day].strftime("%A")

        if day_name in preferences.fixed_rest_days:
            for workout in range(num_entries):
                model.Add(
                    schedule_vars[(workout, day)] == 0
                )  # make the day workout value pair false

    # max workouts per day
    for day in range(num_days):
        model.Add(
            (sum(schedule_vars[(w, day)] for w in range(num_entries)))
            <= preferences.max_workouts_per_day
        )


def _add_muscle_gap_constraints(model, schedule_vars, w1, w2, num_days, min_days):
    for day in range(num_days):
        for gap in range(0, min_days):
            if day + gap >= num_days:
                continue

            if gap == 0:
                check = schedule_vars[(w1, day)] + schedule_vars[(w2, day)]
                model.Add(check <= 1)
            else:
                check1 = schedule_vars[(w1, day)] + schedule_vars[(w2, day + gap)]
                check2 = schedule_vars[(w2, day)] + schedule_vars[(w1, day + gap)]
                model.Add(check1 <= 1)
                model.Add(check2 <= 1)


def _apply_muscle_rest_constraint(
    model, schedule_vars, all_entries, num_entries, num_days, min_rest_hours
):
    min_days = int(min_rest_hours / 24)

    if min_days <= 0:
        return
    for w1 in range(num_entries):
        for w2 in range(w1 + 1, num_entries):
            first_muscles = set(all_entries[w1].primary_muscles)
            second_muscles = set(all_entries[w2].primary_muscles)
            if first_muscles.intersection(second_muscles):
                _add_muscle_gap_constraints(
                    model, schedule_vars, w1, w2, num_days, min_days
                )


def _set_penalties(
    model, schedule_vars, all_entries, num_entries, num_days, available_days
):
    # penalties
    penalties = []
    DROP_PENALTY = 100

    for workout in range(num_entries):
        is_scheduled = sum(schedule_vars[(workout, day)] for day in range(num_days))

        is_dropped = model.NewBoolVar(f"drop_{workout}")
        model.Add(is_dropped == 1 - is_scheduled)

        penalties.append(is_dropped * DROP_PENALTY)

        original_date = all_entries[workout].scheduled_at.date()
        for day in range(num_days):
            new_date = available_days[day].date()
            diff = abs((new_date - original_date).days)
            penalties.append(diff * schedule_vars[(workout, day)])

    model.Minimize(sum(penalties))


def _extract_results(
    solver, schedule_vars, all_entries, num_entries, num_days, available_days
):
    rescheduled_entries = []
    dropped_entries = []

    for workout in range(num_entries):
        rescheduled = False

        for day in range(num_days):
            if solver.Value(schedule_vars[(workout, day)]) == 1:
                rescheduled = True
                entry = all_entries[workout]

                new_datetime = available_days[day].replace(
                    hour=entry.scheduled_at.hour, minute=entry.scheduled_at.minute
                )

                if new_datetime != entry.scheduled_at or entry.status == "Missed":
                    rescheduled_entries.append(
                        RescheduledEntry(
                            entry_id=entry.id,
                            workout_id=entry.workout_id,
                            workout_name=entry.workout_name,
                            original_scheduled_at=entry.scheduled_at,
                            new_scheduled_at=new_datetime,
                            action="Shifted",
                        )
                    )
                break

        if not rescheduled and all_entries[workout].status == "Missed":
            dropped_entries.append(
                RescheduledEntry(
                    entry_id=all_entries[workout].id,
                    workout_id=all_entries[workout].workout_id,
                    workout_name=all_entries[workout].workout_name,
                    original_scheduled_at=all_entries[workout].scheduled_at,
                    new_scheduled_at=all_entries[workout].scheduled_at,
                    action="Dropped",
                )
            )

    return rescheduled_entries, dropped_entries


def attempt_tier_two(
    request: RescheduleRequest, start_time: float
) -> Optional[RescheduleResponse]:
    model = cp_model.CpModel()

    available_days = _get_available_days(
        request.planning_window_start, request.planning_window_end
    )
    num_days = len(available_days)

    all_entries = request.entries
    num_entries = len(all_entries)

    schedule_vars = {}

    # add variables for the model to solve - basically combinations of workout,day  boolean pairs e.g Monday Push = 0/1
    for workout in range(num_entries):
        for day in range(num_days):
            var_name = f"workout_{workout}_on_day_{day}"
            schedule_vars[(workout, day)] = model.NewBoolVar(var_name)

    # apply constraints and objective via helpers - thanks sonarqube -.-
    _apply_basic_constraints(
        model, schedule_vars, num_entries, num_days, available_days, request.preferences
    )
    _apply_muscle_rest_constraint(
        model,
        schedule_vars,
        all_entries,
        num_entries,
        num_days,
        request.preferences.min_muscle_rest_hours,
    )
    _set_penalties(
        model, schedule_vars, all_entries, num_entries, num_days, available_days
    )

    # solving step, given a max of 2 seonds
    solver = cp_model.CpSolver()
    solver.parameters.max_time_in_seconds = 2.0

    status = solver.Solve(model)

    if status == cp_model.OPTIMAL or status == cp_model.FEASIBLE:
        rescheduled_entries, dropped_entries = _extract_results(
            solver, schedule_vars, all_entries, num_entries, num_days, available_days
        )

        # everything got dropped aka not viable
        if len(rescheduled_entries) == 0:
            return None

        return RescheduleResponse(
            user_id=request.user_id,
            execution_tier="Tier2_CPSAT",
            execution_time_ms=int((time.time() - start_time) * 1000),
            rescheduled_entries=rescheduled_entries,
            dropped_entries=dropped_entries,
        )
    else:
        return None
