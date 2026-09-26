using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Domain.Gamification;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Gamification.Rules;

public sealed class DuelWinsRule : IBadgeRule
{
    private readonly OptiLiftsDbContext _db;

    public DuelWinsRule(OptiLiftsDbContext db) => _db = db;

    public string Code => "duel_wins";

    public async Task<bool> IsEarnedAsync(Guid userId, Badge badge, CancellationToken cancellationToken)
    {
        var threshold = badge.Threshold ?? 5;
        var wins = await _db.Duels
            .AsNoTracking()
            .CountAsync(d => d.WinnerUserId == userId && d.Status == "Finished", cancellationToken);
        return wins >= threshold;
    }
}
