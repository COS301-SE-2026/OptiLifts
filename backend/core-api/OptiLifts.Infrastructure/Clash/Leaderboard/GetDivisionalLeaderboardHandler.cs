using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class GetDivisionalLeaderboardHandler : IRequestHandler<GetDivisionalLeaderboardQuery, LeaderboardPageResult>
{
    private readonly OptiLiftsDbContext _db;

    public GetDivisionalLeaderboardHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<LeaderboardPageResult> Handle(GetDivisionalLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, LeaderboardMetrics.maxPageSize);

        if (!WeightClassBrackets.TryGetRange(request.BracketId, out var minExclusive, out var maxInclusive))
        {
            return new LeaderboardPageResult(Array.Empty<LeaderboardEntryDto>(), 0, page, pageSize, null);
        }

        var currDate = DateTime.UtcNow;
        var ssnKey = currDate.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var ssnStart = new DateTime(currDate.Year, currDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var normalisedGender = string.Equals(request.Gender, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";

        var baseQuery = _db.AthleteSeasonSnapshots
            .AsNoTracking()
            .Where(s => s.SeasonKey == ssnKey
                && s.IsOptedIn
                && s.LastWorkoutDate >= ssnStart
                && s.Gender == normalisedGender
                && s.BodyweightKg > minExclusive
                && s.BodyweightKg <= maxInclusive);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var pageEntities = await LeaderboardMetrics.ApplyOrder(baseQuery, request.Metric)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var entries = pageEntities.Select((s, index) => LeaderboardMetrics.ToDto(s, (page - 1) * pageSize + index + 1, request.RequestingUserId)).ToList();

        var currUserEntry = await LeaderboardMetrics.BuildCurrentUserEntryAsync(
            _db, baseQuery, request.Metric, request.RequestingUserId, ssnKey, cancellationToken);

        var isUserOptedIn = await _db.Users.Where(u => u.Id == request.RequestingUserId).Select(u => u.GlobalLeaderboardOptIn).FirstOrDefaultAsync(cancellationToken);

        return new LeaderboardPageResult(entries, totalCount, page, pageSize, currUserEntry, isUserOptedIn);
    }
}
