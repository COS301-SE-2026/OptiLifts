from ortools.sat.python import cp_model
from typing import Optional
from datetime import timedelta
import time

from app.models.reschedule import RescheduleRequest, RescheduleResponse, RescheduledEntry

def attempt_tier_two(request: RescheduleRequest, start_time: float) -> Optional[RescheduleResponse]:
    model = cp_model.CpModel()

    available_days = []
    curr_day = request.planning_window_start
    while (curr_day.date() <= request.planning_window_end.date()):
        available_days.append(curr_day)
        curr_day += timedelta(days=1)

    num_days = len(available_days)

    all_entires = request.entries
    num_entries = len(all_entires)

    schedule_vars = {}

    # add variables for the model to solve - basically combinations of workout,day  boolean pairs e.g Monday Push = 0/1
    for workout in range(num_entries):
        for day in range(num_days):
            var_name = f"workout_{workout}_on_day_{day}"
            schedule_vars[(workout, day)] = model.NewBoolVar(var_name)


    # constraints
    #every workout scheduled once
    for workout in range(num_entries):
        model.AddExactlyOne(schedule_vars[(workout, day)] for day in range(num_days))

    # rest days 
    for day in range(num_days):
        day_name = available_days[day].strftime("%A")

        if (day_name in request.preferences.fixed_rest_days):
            for workout in range(num_entries):
                model.Add(schedule_vars[(workout, day)] == 0) #make the day workout value pair false

    # max workouts per day
    for day in range(num_days):
        model.Add((sum(schedule_vars[(w, day)] for w in range(num_entries))) <= request.preferences.max_workouts_per_day)

    # min rest between training muscle
    if request.preferences.min_muscle_rest_hours >= 48:
        for w1 in range(num_entries):
            for w2 in range(w1+1, num_entries):
                first_muscles = set(all_entires[w1].primary_muscles)
                second_muscles = set(all_entires[w2].primary_muscles)

                if first_muscles.intersection(second_muscles):
                    for day in range(num_days - 1):
                        check1 = schedule_vars[(w1, day)] + schedule_vars[(w2, day + 1)]
                        check2 = schedule_vars[(w2, day)] + schedule_vars[(w1, day + 1)]

                        model.Add(check1 <= 1)
                        model.Add(check2 <= 1)




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

    