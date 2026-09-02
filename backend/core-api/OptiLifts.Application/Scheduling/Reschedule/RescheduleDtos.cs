using System.Text.Json.Serialization;

namespace OptiLifts.Application.Scheduling.Reschedule;

public record UserScheduleConfigDto(
    bool DynamicSchedulerEnabled,
    int MaxWorkoutsPerDay,
    int MinMuscleRestHours,
    List<string> RestDays,
    int CycleWindowLengthDays,
    DateTime CycleStartDate
);
public record RescheduleRequestDto(
    List<Guid> SelectedMissedEntryIds
);
public record RescheduledEntryDto(
    Guid EntryId,
    Guid WorkoutId,
    string WorkoutName,
    DateTime OriginalScheduledAt,
    DateTime NewScheduledAt,
    string Action
);
public record RescheduleEntryDetailDto(
    Guid Id,
    Guid WorkoutId,
    string WorkoutName,
    DateTime ScheduledAt,
    string Status,
    List<string> PrimaryMuscles
);
public record RescheduleResultDto(
    Guid UserId,
    string ExecutionTier,
    int ExecutionTimeMs,
    List<RescheduledEntryDto> RescheduledEntries,
    List<RescheduledEntryDto> DroppedEntries
);

public record ConfirmRescheduleItemDto(
    Guid EntryId,
    DateTime NewScheduledAt
);