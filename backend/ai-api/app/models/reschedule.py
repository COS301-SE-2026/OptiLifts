from pydantic import BaseModel, Field
from typing import List
from datetime import datetime

class Preferences(BaseModel):
    max_workouts_per_day: int = Field(default=1, ge=1)
    min_muscle_rest_hours: int = Field(default=48, ge=0)
    fixed_rest_days: List[str] = Field(default_factory=list)

class Entry(BaseModel):
    id: str
    wokrout_id: str
    workout_name: str
    scheduled_at: datetime
    status: str
    primary_muscles: List[str] = Field(default_factory=list)
    estimated_duration_minutes: int = Field(default=60, ge=0)


class RescheduleRequest(BaseModel):
    user_id: str
    planning_window_start: datetime
    planning_window_end: datetime
    preferences: Preferences
    entries: List[Entry] = Field(default_factory=list)

class RescheduledEntry(BaseModel):
    entry_id: str
    workout_id: str
    workout_name: str
    original_scheduled_at: datetime
    new_scheduled_at: datetime
    action: str

class RescheduleResponse(BaseModel):
    user_id: str
    execution_tier: str   
    execution_time_ms: int   # for fun analytics
    rescheduled_entries: List[RescheduledEntry]
    dropped_entries: List[Entry]