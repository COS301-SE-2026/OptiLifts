using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Domain.Gamification;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Gamification.Rules;

public sealed class SeasonRankRule : IBadgeRule
{
    private readonly OptiLiftsDbContext _db;

    public SeasonRankRule(OptiLiftsDbContext db) => _db = db;

    public string Code => "season_rank";

    public async Task<bool> IsEarnedAsync(Guid userId, Badge badge, CancellationToken cancellationToken)
    {
        var topUser = await _db.AthleteSeasonSnapshots
            .AsNoTracking()
            .Where(s => s.IsOptedIn)
            .OrderByDescending(s => s.DotsScore)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        return topUser == userId && topUser != Guid.Empty;
    }
}
