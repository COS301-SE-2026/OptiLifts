namespace OptiLifts.Application.Clash.Arenas;

public sealed record ArenaDto(
    string Id,
    string Name,
    string Type,
    string? Code,
    Guid? CreatedById,
    string MetricType,
    int DurationDays,
    DateTime SeasonEndDate,
    DateTime CreatedAt,
    int MemberCount,
    bool IsActive,
    int DaysRemaining,
    string? UserRole,
    bool IsUserMember
);

public sealed record CreateArenaResult(
    bool Success,
    string Message,
    ArenaDto? Arena = null
);

public sealed record JoinArenaResult(
    bool Success,
    string Message,
    ArenaDto? Arena = null
);

public sealed record InviteFriendToArenaResult(
    bool Success,
    string Message,
    Guid? InviteId = null
);

public sealed record SendActivityKudosResult(
    bool Success,
    int KudosCount,
    string Message
);
