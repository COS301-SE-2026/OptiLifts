using MediatR;

namespace OptiLifts.Application.Clash.Duels.Queries;

public sealed record GetDuelDetailQuery(Guid UserId, Guid DuelId) : IRequest<DuelDetailDto?>;

public sealed record DuelTimelineEventDto(Guid Id, Guid UserId, string UserName, string EventText, bool IsPr, DateTime CreatedAt);

public sealed record DuelDetailDto(
    Guid Id,
    string Title,
    Guid ChallengerUserId,
    string ChallengerName,
    string? ChallengerAvatarUrl,
    Guid RivalUserId,
    string RivalName,
    string? RivalAvatarUrl,
    string ExerciseName,
    string TargetType,
    string Status,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal ChallengerCurrentValue,
    decimal RivalCurrentValue,
    decimal? ChallengerBaselineValue,
    decimal? RivalBaselineValue,
    Guid? WinnerUserId,
    bool IsDraw,
    IReadOnlyList<DuelTimelineEventDto> Timeline
);
