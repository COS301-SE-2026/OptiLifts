using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Queries;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class GetGlobalLeaderboardHandler : IRequestHandler<GetGlobalLeaderboardQuery, LeaderboardPageResult>
{
    private readonly OptiLiftsDbContext _db;

    public GetGlobalLeaderboardHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<LeaderboardPageResult> Handle(GetGlobalLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, LeaderboardMetrics.maxPageSize);

        var now = DateTime.UtcNow;
        var ssnKey = now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var ssnStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var isAllTime = string.Equals(request.Timeframe, "all-time", StringComparison.OrdinalIgnoreCase);

        var baseQuery = _db.AthleteSeasonSnapshots
            .AsNoTracking()
            .Where(s => s.SeasonKey == ssnKey && s.IsOptedIn);

        if (!isAllTime)
        {
            baseQuery = baseQuery.Where(s => s.LastWorkoutDate >= ssnStart);
        }
        Dictionary<Guid, decimal>? allTimeVolumes = null;
        if (isAllTime)
        {
            allTimeVolumes = await _db.WorkoutLogSets.AsNoTracking()
            .Join(_db.WorkoutLogs, s => s.LogId, l => l.Id, (s, l) => new { Set = s, Log = l })
            .Join(_db.ScheduledEntries, x => x.Log.EntryId, e => e.Id, (x, e) => new { x.Set, x.Log, Entry = e })
            .Where(x => x.Set.Type == SetType.Normal && x.Log.CompletedAt != null)
            .GroupBy(x => x.Entry.UserId)
            .Select(g => new { UserId = g.Key, Volume = (decimal)g.Sum(x => x.Set.Weight * x.Set.Reps) })
            .ToDictionaryAsync(x => x.UserId, x => x.Volume, cancellationToken);
        }
        if (isAllTime && string.Equals(request.Metric, "TotalVolume", StringComparison.OrdinalIgnoreCase))
        {
            var allEntities = await baseQuery.ToListAsync(cancellationToken);
            foreach (var entity in allEntities)
            {
                entity.WeeklyVolumeKg = allTimeVolumes?.GetValueOrDefault(entity.UserId, 0m) ?? 0m;
            }
            var count = allEntities.Count;
            var ordered = allEntities.OrderByDescending(s => s.WeeklyVolumeKg).ThenBy(s => s.DisplayName).ToList();
            var pageEntities = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var entries = pageEntities.Select((s, index) => LeaderboardMetrics.ToDto(s, (page - 1) * pageSize + index + 1, request.RequestingUserId)).ToList();

            var selfEntity = ordered.FirstOrDefault(s => s.UserId == request.RequestingUserId);
            var selfRank = selfEntity != null ? ordered.IndexOf(selfEntity) + 1 : 0;
            var currentUserEntry = selfEntity != null ? LeaderboardMetrics.ToDto(selfEntity, selfRank, request.RequestingUserId) : null;

            var isoptedin = await _db.Users.Where(u => u.Id == request.RequestingUserId).Select(u => u.GlobalLeaderboardOptIn).FirstOrDefaultAsync(cancellationToken);

            return new LeaderboardPageResult(entries, count, page, pageSize, currentUserEntry, isoptedin);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var pageEntitiesstd = await LeaderboardMetrics.ApplyOrder(baseQuery, request.Metric)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        if (isAllTime && allTimeVolumes != null)
        {
            foreach (var e in pageEntitiesstd)
            {
                e.WeeklyVolumeKg = allTimeVolumes.GetValueOrDefault(e.UserId, 0m);
            }
        }

        var entriesstd = pageEntitiesstd
            .Select((s, index) => LeaderboardMetrics.ToDto(s, (page - 1) * pageSize + index + 1, request.RequestingUserId)).ToList();

        var currUserEntry = await LeaderboardMetrics.BuildCurrentUserEntryAsync(
            _db, baseQuery, request.Metric, request.RequestingUserId, ssnKey, cancellationToken);

        if (isAllTime && currUserEntry != null && allTimeVolumes != null)
        {
            currUserEntry = currUserEntry with
            {
                WeeklyVolumeKg = allTimeVolumes.GetValueOrDefault(request.RequestingUserId, 0m)
            };
        }

        var isUserOptedIn = await _db.Users.Where(u => u.Id == request.RequestingUserId).Select(u => u.GlobalLeaderboardOptIn).FirstOrDefaultAsync(cancellationToken);

        return new LeaderboardPageResult(entriesstd, totalCount, page, pageSize, currUserEntry, isUserOptedIn);
    }
}
