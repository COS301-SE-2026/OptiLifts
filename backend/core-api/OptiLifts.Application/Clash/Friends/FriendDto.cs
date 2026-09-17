namespace OptiLifts.Application.Clash.Friends;

public sealed record FriendDto(
    Guid Id,
    string Name,
    string Initials,
    string? AvatarUrl,
    string Code,
    decimal DotsScore,
    string Tier
);
public sealed record FriendRequestDto(
    Guid Id,
    Guid FromAthleteId,
    string FromName,
    string FromInitials,
    string? FromAvatarUrl,
    string FromCode,
    DateTime SentAt
);

public sealed record PendingFriendRequestsResult(
    IReadOnlyList<FriendRequestDto> Incoming,
    IReadOnlyList<FriendRequestDto> Outgoing
);
