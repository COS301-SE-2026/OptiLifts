using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Queries;
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

        var baseQuery = _db.AthleteSeasonSnapshots
            .AsNoTracking()
            .Where(s => s.SeasonKey == ssnKey && s.IsOptedIn && s.LastWorkoutDate >= ssnStart);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var pageEntities = await LeaderboardMetrics.ApplyOrder(baseQuery, request.Metric)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var entries = pageEntities
            .Select((s, index) => LeaderboardMetrics.ToDto(s, (page - 1) * pageSize + index + 1, request.RequestingUserId)).ToList();

        var currUserEntry = await LeaderboardMetrics.BuildCurrentUserEntryAsync(
            _db, baseQuery, request.Metric, request.RequestingUserId, ssnKey, cancellationToken);

            var isUserOptedIn = await _db.Users.Where(u => u.Id == request.RequestingUserId).Select(u => u.GlobalLeaderboardOptIn).FirstOrDefaultAsync(cancellationToken);

        return new LeaderboardPageResult(entries, totalCount, page, pageSize, currUserEntry, isUserOptedIn);
    }
}
