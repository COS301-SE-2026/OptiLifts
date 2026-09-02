namespace OptiLifts.Infrastructure.Scheduling.Reschedule;

internal record PythonRescheduleRequest(
    string UserId,
    DateTime PlanningWindowStart,
    DateTime PlanningWindowEnd,
    PythonPreferences Preferences,
    List<PythonEntry> Entries
);
internal record PythonPreferences(
    int MaxWorkoutsPerDay,
    int MinMuscleRestHours,
    List<string> FixedRestDays
);
internal record PythonEntry(
    string Id,
    string WorkoutId,
    string WorkoutName,
    DateTime ScheduledAt,
    string Status,
    List<string> PrimaryMuscles
);
internal record PythonRescheduleResponse(
    string UserId,
    string ExecutionTier,
    int ExecutionTimeMs,
    List<PythonRescheduledEntry> RescheduledEntries,
    List<PythonRescheduledEntry> DroppedEntries
);
internal record PythonRescheduledEntry(
    string EntryId,
    string WorkoutId,
    string WorkoutName,
    DateTime OriginalScheduledAt,
    DateTime NewScheduledAt,
    string Action
);