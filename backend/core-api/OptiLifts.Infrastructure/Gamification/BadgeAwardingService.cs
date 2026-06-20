using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Domain.Gamification;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Gamification;

public sealed class BadgeAwardingService : IBadgeAwardingService
{
    private readonly OptiLiftsDbContext _db;
    private readonly IReadOnlyDictionary<string, IBadgeRule> _rules;

    public BadgeAwardingService(OptiLiftsDbContext db, IEnumerable<IBadgeRule> rules)
    {
        _db = db;
        _rules = rules.ToDictionary(r => r.Code);
    }

    public async Task AwardEligibleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var earnedBadgeIds = await _db.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync(cancellationToken);

        var candidates = await _db.Badges
                .Where(b => !earnedBadgeIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

        var newlyEarned = false;
        foreach (var badge in candidates)
        {
            if (_rules.TryGetValue(badge.Code, out var rule) &&
                await rule.IsEarnedAsync(userId, badge, cancellationToken))
            {
                _db.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = badge.Id });
                newlyEarned = true;
            }
        }

        if (newlyEarned)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}