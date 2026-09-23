using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

internal static class LeaderboardMetrics
{
    public const string totalVal = "TotalVolume";
    public const string sqtE1RM = "SquatE1RM";
    public const string bnchE1RM = "BenchE1RM";
    public const string dlftE1RM = "DeadliftE1RM";
    public const int maxPageSize = 100;

    public static IOrderedQueryable<AthleteSeasonSnapshot> ApplyOrder(IQueryable<AthleteSeasonSnapshot> query, string metric)
    {
        return metric switch
        {
            totalVal => query.OrderByDescending(s => s.WeeklyVolumeKg),
            sqtE1RM => query.OrderByDescending(s => s.Squat1RM),
            bnchE1RM => query.OrderByDescending(s => s.Bench1RM),
            dlftE1RM => query.OrderByDescending(s => s.Deadlift1RM),
            _ => query.OrderByDescending(s => s.DotsScore)
        };
    }

    public static decimal GetMetricVal(AthleteSeasonSnapshot snapshot, string metric)
    {
        return metric switch
        {
            totalVal => snapshot.WeeklyVolumeKg,
            sqtE1RM => snapshot.Squat1RM,
            bnchE1RM => snapshot.Bench1RM,
            dlftE1RM => snapshot.Deadlift1RM,
            _ => snapshot.DotsScore
        };
    }

    public static async Task<int> ComputeRankAsync(
        IQueryable<AthleteSeasonSnapshot> filteredQuery,
        string metric,
        decimal currentValue,
        CancellationToken cancellationToken)
    {
        var higherCount = metric switch
        {
            totalVal => await filteredQuery.CountAsync(s => s.WeeklyVolumeKg > currentValue, cancellationToken),
            sqtE1RM => await filteredQuery.CountAsync(s => s.Squat1RM > currentValue, cancellationToken),
            bnchE1RM => await filteredQuery.CountAsync(s => s.Bench1RM > currentValue, cancellationToken),
            dlftE1RM => await filteredQuery.CountAsync(s => s.Deadlift1RM > currentValue, cancellationToken),
            _ => await filteredQuery.CountAsync(s => s.DotsScore > currentValue, cancellationToken)
        };

        return higherCount + 1;
    }

    public static LeaderboardEntryDto ToDto(AthleteSeasonSnapshot s, int rank, Guid requestingUserId)
    {
        return new LeaderboardEntryDto(
            s.UserId,
            rank,
            s.DisplayName,
            s.AvatarUrl,
            s.BodyweightKg,
            s.Tier,
            s.TierLevel,
            s.DotsScore,
            s.Squat1RM,
            s.Bench1RM,
            s.Deadlift1RM,
            s.TotalE1RM,
            s.WeeklyVolumeKg,
            s.RankTrend,
            s.UserId == requestingUserId);
    }

    public static async Task<LeaderboardEntryDto?> BuildCurrentUserEntryAsync(
        OptiLiftsDbContext db,
        IQueryable<AthleteSeasonSnapshot> filteredQuery,
        string metric,
        Guid userId,
        string seasonKey,
        CancellationToken cancellationToken)
    {
        var self = await db.AthleteSeasonSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeasonKey == seasonKey, cancellationToken);

        if (self is null)
        {
            return null;
        }

        var currVal = GetMetricVal(self, metric);
        var rank = await ComputeRankAsync(filteredQuery, metric, currVal, cancellationToken);

        return ToDto(self, rank, userId);
    }
}
