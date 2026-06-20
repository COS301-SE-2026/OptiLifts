using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Domain.Gamification;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Gamification.Rules;

//generic with N workouts with code "workout_count" and threshold
public sealed class WorkoutCountRule : IBadgeRule
{
    private readonly OptiLiftsDbContext _db;
    public WorkoutCountRule(OptiLiftsDbContext db) => _db = db;
    public string Code => "workout_count";
    public async Task<bool> IsEarnedAsync(Guid userId, Badge badge, CancellationToken cancellationToken)
    {
        var count = await _db.Workouts.CountAsync(w => w.CreatedBy == userId, cancellationToken);
        return count >= (badge.Threshold ?? int.MaxValue);
    }
}