using MediatR;

namespace OptiLifts.Application.Clash.Duels.Queries;

public sealed record GetUserDuelsQuery(Guid UserId, string? FilterStatus) : IRequest<UserDuelsResult>;

public sealed record DuelSummaryDto(
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
    bool IsDraw
);

public sealed record UserDuelsResult(
    IReadOnlyList<DuelSummaryDto> Duels,
    int Won,
    int Lost,
    int Active,
    decimal WinRatePercent
);
