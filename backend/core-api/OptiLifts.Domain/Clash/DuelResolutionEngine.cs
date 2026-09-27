namespace OptiLifts.Domain.Clash;

public static class DuelResolutionEngine
{
    private const string E1RMGainPercent = "E1RMGainPercent";

    public static bool TryResolve(Duel duel, DateTime utcNow)
    {
        if (duel.Status != "Active" || duel.EndDate is null || duel.EndDate > utcNow)
        {
            return false;
        }

        var challengerScore = DoScore(duel.TargetType, duel.ChallengerBaselineValue, duel.ChallengerCurrentValue);
        var rivalScore = DoScore(duel.TargetType, duel.RivalBaselineValue, duel.RivalCurrentValue);

        duel.Status = "Finished";

        if (challengerScore == rivalScore)
        {
            duel.IsDraw = true;
            duel.WinnerUserId = null;
        }
        else if (challengerScore > rivalScore)
        {
            duel.WinnerUserId = duel.ChallengerUserId;
        }
        else
        {
            duel.WinnerUserId = duel.RivalUserId;
        }

        return true;
    }

    private static decimal DoScore(string targetType, decimal? baselineValue, decimal currentValue)
    {
        if (targetType != E1RMGainPercent)
        {
            return currentValue;
        }

        if (!baselineValue.HasValue || baselineValue.Value <= 0)
        {
            return 0m;
        }

        return (currentValue - baselineValue.Value) / baselineValue.Value * 100m;
    }
}
