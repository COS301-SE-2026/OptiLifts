namespace OptiLifts.Application.Scheduling.GetScheduleAnalytics;

public sealed record ScheduleAnalyticsDto(
    int TotalWorkouts,
    float TotalVolume,
    int TotalSets,
    IReadOnlyList<MuscleDistributionDto> MuscleDistribution
);
public sealed record MuscleDistributionDto(
    string MuscleGroup,
    int SetCount,
    float Percentage
);

