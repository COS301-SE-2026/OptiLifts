namespace OptiLifts.Application.Clash.Duels;

public sealed record DuelUpdateDto(
    Guid DuelId,
    decimal ChallengerCurrentValue,
    decimal RivalCurrentValue,
    string LatestEventText,
    bool IsPr
);
