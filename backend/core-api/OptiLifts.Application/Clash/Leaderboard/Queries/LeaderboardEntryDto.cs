namespace OptiLifts.Application.Clash.Leaderboard.Queries;

public sealed record LeaderboardEntryDto(
    Guid UserId,
    int Rank,
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
    int RankTrend,
    bool IsCurrentUser
);
