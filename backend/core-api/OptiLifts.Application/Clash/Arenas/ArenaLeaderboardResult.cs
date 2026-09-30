namespace OptiLifts.Application.Clash.Arenas;

public sealed record ArenaLeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string DisplayName,
    string Initials,
    string? AvatarUrl,
    string Gender,
    decimal BodyweightKg,
    decimal Score,
    decimal DotsScore,
    decimal TotalE1RM,
    decimal Squat1RM,
    decimal Bench1RM,
    decimal Deadlift1RM,
    decimal WeeklyVolumeKg,
    string Tier,
    int TierLevel,
    int Trend,
    string Role,
    bool IsCurrentUser
);

public sealed record ArenaLeaderboardResult(
    ArenaDto Arena,
    IReadOnlyList<ArenaLeaderboardEntryDto> Standings,
    ArenaLeaderboardEntryDto? UserStanding
);
