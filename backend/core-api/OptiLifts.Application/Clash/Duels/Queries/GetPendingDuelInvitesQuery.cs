using MediatR;

namespace OptiLifts.Application.Clash.Duels.Queries;

public sealed record GetPendingDuelInvitesQuery(Guid UserId) : IRequest<IReadOnlyList<DuelInviteDto>>;

public sealed record DuelInviteDto(
    Guid Id,
    Guid ChallengerUserId,
    string ChallengerName,
    string? ChallengerAvatarUrl,
    string ExerciseName,
    string TargetType,
    int DurationDays,
    DateTime CreatedAt
);
