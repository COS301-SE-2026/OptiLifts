namespace OptiLifts.Application.Scheduling.GetSchedule;

public sealed record ScheduledEntryDto(
    Guid Id,
    Guid WorkoutId,
    string WorkoutName,
    DateTime Scheduled,
    string Status
);