namespace OptiLifts.Application.Scheduling.GetSchedule;

public sealed record ScheduledEntryDto(
    Guid Id,
    Guid WorkoutId,
    string WorkoutName,
    DateTime Scheduled,
    string Status,
    string[] PrimaryMuscleGroups,
    int ExerciseCount,
    string[] ExercisePreview,
    float TotalVolume,
    int TotalSets,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? RecordCount,
    Guid? LogId
);