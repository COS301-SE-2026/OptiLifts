using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Domain.Gamification;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Gamification.Rules;

public sealed class DotsScoreRule : IBadgeRule
{
    private readonly OptiLiftsDbContext _db;

    public DotsScoreRule(OptiLiftsDbContext db) => _db = db;

    public string Code => "dots_score";

    public async Task<bool> IsEarnedAsync(Guid userId, Badge badge, CancellationToken cancellationToken)
    {
        var threshold = badge.Threshold ?? 320;
        return await _db.AthleteSeasonSnapshots
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.DotsScore >= threshold, cancellationToken);
    }
}
