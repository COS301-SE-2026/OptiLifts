using MediatR;

namespace OptiLifts.Application.Clash.Athletes.Queries;

public sealed record GetAthleteProfileQuery(Guid AthleteId, Guid RequestingUserId) : IRequest<AthleteProfileResult?>;

public sealed record MuscleBalanceDto(string MuscleGroup, decimal VolumeKg);

public sealed record TrophyDto(Guid Id, string Name, string Category, string Description, string? IconUrl, DateTime EarnedAt);

public sealed record RecentWorkoutExerciseDto(string ExerciseName, int Sets);

public sealed record RecentWorkoutDto(
    Guid LogId,
    string Title,
    string? Notes,
    DateTime? CompletedAt,
    decimal VolumeKg,
    IReadOnlyList<RecentWorkoutExerciseDto> Exercises
);

public sealed record AthleteProfileResult(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    decimal BodyweightKg,
    string Tier,
    int TierLevel,
    decimal DotsScore,
    decimal Squat1RM,
    decimal Bench1RM,
    decimal Deadlift1RM,
    decimal TotalE1RM,
    decimal WeeklyVolumeKg,
    IReadOnlyList<MuscleBalanceDto> MuscleBalance30d,
    IReadOnlyList<TrophyDto> Trophies,
    IReadOnlyList<RecentWorkoutDto> RecentWorkouts,
    int KudosCount,
    bool HasSentKudos
);
