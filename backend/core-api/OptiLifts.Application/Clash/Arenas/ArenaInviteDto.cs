namespace OptiLifts.Application.Clash.Arenas;

public sealed record ArenaInviteDto(
    Guid Id,
    string ArenaId,
    string ArenaName,
    string? ArenaCode,
    Guid InvitedByUserId,
    string InvitedByName,
    string InvitedByInitials,
    string? InvitedByAvatarUrl,
    string Status,
    DateTime CreatedAt
);
