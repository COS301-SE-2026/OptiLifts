namespace OptiLifts.Application.Clash.Arenas;

public sealed record ClashActivityDto(
    Guid Id,
    string ArenaId,
    Guid UserId,
    string UserName,
    string UserInitials,
    string? UserAvatarUrl,
    string EventText,
    string Details,
    bool IsPr,
    bool IsPromotion,
    int KudosCount,
    bool HasUserKudoed,
    DateTime CreatedAt
);
