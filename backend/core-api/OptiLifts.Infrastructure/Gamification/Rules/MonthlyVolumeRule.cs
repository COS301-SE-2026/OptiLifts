using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Domain.Gamification;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Gamification.Rules;

public sealed class MonthlyVolumeRule : IBadgeRule
{
    private readonly OptiLiftsDbContext _db;

    public MonthlyVolumeRule(OptiLiftsDbContext db) => _db = db;

    public string Code => "monthly_volume";

    public async Task<bool> IsEarnedAsync(Guid userId, Badge badge, CancellationToken cancellationToken)
    {
        var threshold = badge.Threshold ?? 100000;
        return await _db.AthleteSeasonSnapshots
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.WeeklyVolumeKg >= threshold, cancellationToken);
    }
}
