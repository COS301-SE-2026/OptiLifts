import pytest 
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
            "fixed_rest_days": ["Sunday"]
        },
        "entries": []
    }
def create_entry(id_str, status, date_str, muscles=["Trapazoids"]):
    return {
        "id": id_str,
        "workout_id": f"w-{id_str}",
        "workout_name": f"Workout {id_str}",
        "scheduled_at": date_str,
        "status": status,
        "primary_muscles": muscles,
        "estimated_duration_minutes": 60
    }


def test_tier1_1missedworkout_openweek_success():
    payload = get_base_payload()
    payload["entries"].append(create_entry("1", "Missed", "2026-08-31T08:00:00Z"))
    
    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200
    
    data = response.json()
    
    assert data["execution_tier"] == "Tier1_FastPath"
    assert data["rescheduled_entries"][0]["new_scheduled_at"] == "2026-08-31T08:00:00Z"

def test_tier1_1missedworkout_rest_day_skip_success():
    payload = get_base_payload()
    
    payload["planning_window_start"] = "2026-09-06T00:00:00Z" 
    payload["planning_window_end"] = "2026-09-13T23:59:59Z"
    payload["entries"].append(create_entry("1", "Missed", "2026-09-06T08:00:00Z"))
    
    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200
    data = response.json()
    
    # should be on Monday 7th
    assert data["rescheduled_entries"][0]["new_scheduled_at"] == "2026-09-07T08:00:00Z"


def test_tier1_1missedworkout_nextdaytaken_success():
    payload = get_base_payload()
    payload["entries"].append(create_entry("1", "Missed", "2026-08-31T08:00:00Z"))
    payload["entries"].append(create_entry("2", "Scheduled", "2026-08-31T17:00:00Z")) 
    
    response = client.post("/ai-api/reschedule", json=payload)

    assert response.status_code == 200

    data = response.json()
    
    # schedules for Tuesday the 9th
    assert data["rescheduled_entries"][0]["new_scheduled_at"] == "2026-09-01T08:00:00Z"








    
