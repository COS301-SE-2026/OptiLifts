from fastapi.testclient import TestClient
from datetime import datetime

from main import app

client = TestClient(app)


def get_base_payload():
    return {
        "user_id": "test-user",
        "planning_window_start": "2026-08-31T00:00:00Z",
        "planning_window_end": "2026-09-06T23:59:59Z",
        "preferences": {
            "max_workouts_per_day": 1,
            "min_muscle_rest_hours": 48,
            "fixed_rest_days": ["Sunday"],
        },
        "entries": [],
    }


def create_entry(id_str, status, date_str, muscles=["Trapazoids"]):
    return {
        "id": id_str,
        "workout_id": f"w-{id_str}",
        "workout_name": f"Workout {id_str}",
        "scheduled_at": date_str,
        "status": status,
        "primary_muscles": muscles,
    }


def test_tier1_1missedworkout_openweek():
    payload = get_base_payload()
    payload["entries"].append(create_entry("1", "Missed", "2026-08-31T08:00:00Z"))

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier1_FastPath"
    assert data["rescheduled_entries"][0]["new_scheduled_at"] == "2026-08-31T08:00:00Z"


def test_tier1_1missedworkout_rest_day_skip():
    payload = get_base_payload()

    payload["planning_window_start"] = "2026-09-06T00:00:00Z"
    payload["planning_window_end"] = "2026-09-13T23:59:59Z"
    payload["entries"].append(create_entry("1", "Missed", "2026-09-06T08:00:00Z"))

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200
    data = response.json()

    # should be on Monday 7th
    assert data["rescheduled_entries"][0]["new_scheduled_at"] == "2026-09-07T08:00:00Z"


def test_tier1_1missedworkout_nextdaytaken():
    payload = get_base_payload()
    payload["entries"].append(create_entry("1", "Missed", "2026-08-31T08:00:00Z"))
    payload["entries"].append(create_entry("2", "Scheduled", "2026-08-31T17:00:00Z"))

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200

    data = response.json()

    # schedules for Tuesday the 9th
    assert data["rescheduled_entries"][0]["new_scheduled_at"] == "2026-09-02T08:00:00Z"


def test_tier1_missedworkout_musclehoursconflict():
    payload = get_base_payload()
    payload["preferences"]["max_workouts_per_day"] = 2
    payload["preferences"]["min_muscle_rest_hours"] = 72
    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Scheduled", "2026-09-01T08:00:00Z", ["Chest"])
    )

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier1_FastPath"
    assert data["rescheduled_entries"][0]["new_scheduled_at"] == "2026-09-04T08:00:00Z"


def test_tier2_musclerest_enforced_fortwowithsamemuscle():
    payload = get_base_payload()
    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Missed", "2026-09-01T08:00:00Z", ["Chest"])
    )

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier2_CPSAT"

    date1 = datetime.fromisoformat(
        data["rescheduled_entries"][0]["new_scheduled_at"].replace("Z", "+00:00")
    )
    date2 = datetime.fromisoformat(
        data["rescheduled_entries"][1]["new_scheduled_at"].replace("Z", "+00:00")
    )

    hours_diff = abs((date1 - date2).total_seconds() / 3600)

    assert hours_diff >= 48.0


def test_tier2_2missedworkouts_maxworkoutsperday1():
    payload = get_base_payload()
    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Missed", "2026-09-01T08:00:00Z", ["Back"])
    )

    payload["entries"].append(
        create_entry("3", "Scheduled", "2026-09-02T08:00:00Z", ["Legs"])
    )
    payload["entries"].append(
        create_entry("4", "Scheduled", "2026-09-03T08:00:00Z", ["Core"])
    )

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier2_CPSAT"
    assert len(data["rescheduled_entries"]) == 2

    new_dates = []
    for entry in data["rescheduled_entries"]:
        new_dates.append(entry["new_scheduled_at"][:10])

    assert len(set(new_dates)) == 2
    assert "2026-09-06" not in new_dates


def test_tier2_3missedworkouts_2perday():
    payload = get_base_payload()

    # 3 day window
    payload["planning_window_end"] = "2026-09-02T23:59:59Z"

    payload["preferences"]["fixed_rest_days"] = ["Tuesday"]
    payload["preferences"]["max_workouts_per_day"] = 2

    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Missed", "2026-08-31T09:00:00Z", ["Back"])
    )
    payload["entries"].append(
        create_entry("3", "Missed", "2026-08-31T10:00:00Z", ["Legs"])
    )

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier2_CPSAT"

    assert len(data["rescheduled_entries"]) == 3

    new_dates = []
    for entry in data["rescheduled_entries"]:
        new_dates.append(entry["new_scheduled_at"][:10])

    assert "2026-09-01" not in new_dates

    assert len(set(new_dates)) < 3


def test_tier2_schedule_muscleconflict_dropsone():
    payload = get_base_payload()
    payload["planning_window_end"] = "2026-09-01T23:59:59Z"

    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Missed", "2026-09-01T08:00:00Z", ["Chest"])
    )

    response = client.post("/ai-api/reschedule", json=payload)
    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier2_CPSAT"
    assert len(data["rescheduled_entries"]) == 1
    assert len(data["dropped_entries"]) == 1


def test_tier2_all_dropped_fails_muscleclash():
    payload = get_base_payload()
    payload["planning_window_end"] = "2026-09-02T23:59:59Z"

    payload["preferences"]["max_workouts_per_day"] = 2

    payload["entries"].append(
        create_entry("1", "Scheduled", "2026-08-31T17:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Scheduled", "2026-09-01T17:00:00Z", ["Quads"])
    )
    payload["entries"].append(
        create_entry("3", "Scheduled", "2026-09-02T17:00:00Z", ["Chest"])
    )

    payload["entries"].append(
        create_entry("4", "Missed", "2026-08-25T08:00:00Z", ["Chest", "Quads"])
    )

    response = client.post("/ai-api/reschedule", json=payload)
    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Failed"


def test_tier2_multiple_drops_required():
    payload = get_base_payload()
    payload["planning_window_end"] = "2026-09-02T23:59:59Z"

    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Missed", "2026-08-31T09:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("3", "Missed", "2026-08-31T10:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("4", "Missed", "2026-08-31T11:00:00Z", ["Chest"])
    )

    response = client.post("/ai-api/reschedule", json=payload)
    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier2_CPSAT"
    assert len(data["rescheduled_entries"]) == 2
    assert len(data["dropped_entries"]) == 2


def test_tier2_distance_penalty_workoutremoval():
    payload = get_base_payload()
    payload["planning_window_end"] = "2026-09-01T23:59:59Z"
    payload["preferences"]["max_workouts_per_day"] = 1

    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Push"])
    )
    payload["entries"].append(
        create_entry("2", "Missed", "2026-09-01T08:00:00Z", ["Pull"])
    )
    payload["entries"].append(
        create_entry("3", "Missed", "2026-09-02T08:00:00Z", ["Legs"])
    )

    response = client.post("/ai-api/reschedule", json=payload)
    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier2_CPSAT"
    assert len(data["rescheduled_entries"]) == 2
    assert len(data["dropped_entries"]) == 1
    assert data["dropped_entries"][0]["entry_id"] == "3"


def test_impossible_schedule_infeasible_4workoutsin3days_max1perday_dropsone():
    payload = get_base_payload()
    payload["planning_window_end"] = "2026-09-02T23:59:59Z"

    payload["entries"].append(
        create_entry("1", "Missed", "2026-08-31T08:00:00Z", ["Chest"])
    )
    payload["entries"].append(
        create_entry("2", "Missed", "2026-08-31T09:00:00Z", ["Back"])
    )
    payload["entries"].append(
        create_entry("3", "Missed", "2026-08-31T10:00:00Z", ["Legs"])
    )
    payload["entries"].append(
        create_entry("4", "Missed", "2026-08-31T10:00:00Z", ["Shoulders"])
    )

    response = client.post("/ai-api/reschedule", json=payload)
    assert response.status_code == 200

    data = response.json()

    assert data["execution_tier"] == "Tier2_CPSAT"
    assert len(data["dropped_entries"]) == 1
    assert len(data["rescheduled_entries"]) == 3


def test_empty_payload():
    payload = get_base_payload()
    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200
    data = response.json()

    assert data["execution_tier"] == "Empty"
    assert len(data["rescheduled_entries"]) == 0
    assert len(data["dropped_entries"]) == 0


def test_missingfields():
    payload = get_base_payload()
    del payload["user_id"]

    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 422
