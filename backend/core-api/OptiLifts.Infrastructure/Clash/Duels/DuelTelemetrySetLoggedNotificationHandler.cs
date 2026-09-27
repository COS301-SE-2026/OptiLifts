using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Duels;
using OptiLifts.Application.Clash.Notifications;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class DuelTelemetrySetLoggedNotificationHandler : INotificationHandler<WorkoutCompletedNotification>
{
    private const int MaxDailyVolumeSets = 30;

    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public DuelTelemetrySetLoggedNotificationHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task Handle(WorkoutCompletedNotification notification, CancellationToken cancellationToken)
    {
        var listOfActiveDuels = await _db.Duels
            .Where(d => d.Status == "Active" && (d.ChallengerUserId == notification.UserId || d.RivalUserId == notification.UserId))
            .ToListAsync(cancellationToken);

        if (listOfActiveDuels.Count == 0)
        {
            return;
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

        if (user is null)
        {
            return;
        }

        foreach (var duel in listOfActiveDuels)
        {
            await ProcessDuelAsync(duel, notification.UserId, notification.LogId, user.DisplayName, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessDuelAsync(Duel duel, Guid userId, Guid logId, string displayName, CancellationToken cancellationToken)
    {
        var sets = await GetQualifiedSetsAsync(duel, logId, cancellationToken);

        if (sets.Count == 0)
        {
            return;
        }

        var isChallenger = duel.ChallengerUserId == userId;

        if (duel.TargetType == "TotalVolumeKg")
        {
            await ProcessVolAsync(duel, isChallenger, sets, userId, displayName, cancellationToken);
        }
        else
        {
            ProcessE1RMGain(duel, isChallenger, sets, userId, displayName);
        }

        var update = new DuelUpdateDto(duel.Id, duel.ChallengerCurrentValue, duel.RivalCurrentValue, $"{displayName} logged a set", false);

        await _notifier.BroadcastDuelUpdateAsync(duel.Id, update, cancellationToken);
    }

    private async Task<List<WorkoutSetLog>> GetQualifiedSetsAsync(Duel duel, Guid logId, CancellationToken cancellationToken)
    {
        var qry = _db.WorkoutLogSets.Where(s => s.LogId == logId && s.Type == SetType.Normal);

        if (duel.ExerciseId.HasValue)
        {
            qry = qry.Where(s => s.ExerciseId == duel.ExerciseId.Value);
        }
        else
        {
            var compoundIds = await _db.Exercises
                .Where(e => !e.IsDeleted && e.UserId == null &&
                    (e.Name == "Barbell Back Squat" || e.Name == "Barbell Bench Press" || e.Name == "Deadlift"))
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);

            qry = qry.Where(s => compoundIds.Contains(s.ExerciseId));
        }

        return await qry.OrderBy(s => s.OrderIndex).ToListAsync(cancellationToken);
    }

    private async Task ProcessVolAsync(Duel duel, bool isChallenger, List<WorkoutSetLog> sets, Guid userId, string displayName, CancellationToken cancellationToken)
    {
        var currDate = DateTime.UtcNow.Date;
        var loggedToday = await _db.DuelTimelineEvents
            .CountAsync(e => e.DuelId == duel.Id && e.UserId == userId && e.CreatedAt >= currDate, cancellationToken);

        var remainingAllowance = Math.Max(0, MaxDailyVolumeSets - loggedToday);
        var countedSets = sets.Take(remainingAllowance).ToList();

        decimal volAdded = 0m;

        foreach (var set in countedSets)
        {
            volAdded += (decimal)(set.Weight * set.Reps);

            _db.DuelTimelineEvents.Add(new DuelTimelineEvent
            {
                DuelId = duel.Id,
                UserId = userId,
                WorkoutLogSetId = set.Id,
                EventText = $"{displayName} logged {set.Weight}kg x {set.Reps}",
                IsPr = false
            });
        }

        if (isChallenger)
        {
            duel.ChallengerCurrentValue += volAdded;
        }
        else
        {
            duel.RivalCurrentValue += volAdded;
        }
    }

    private void ProcessE1RMGain(Duel duel, bool isChallenger, List<WorkoutSetLog> sets, Guid userId, string displayName)
    {
        var baseline = isChallenger ? duel.ChallengerBaselineValue : duel.RivalBaselineValue;
        var currBest = isChallenger ? duel.ChallengerCurrentValue : duel.RivalCurrentValue;

        foreach (var set in sets)
        {
            var e1Rm = (decimal)E1RMCalculationEngine.CalculateE1RM(set.Weight, set.Reps);

            if (e1Rm <= 0)
            {
                continue;
            }

            var isNewPeak = baseline is null || e1Rm > currBest;

            if (baseline is null)
            {
                baseline = e1Rm;
                currBest = e1Rm;
            }
            else if (e1Rm > currBest)
            {
                currBest = e1Rm;
            }

            _db.DuelTimelineEvents.Add(new DuelTimelineEvent
            {
                DuelId = duel.Id,
                UserId = userId,
                WorkoutLogSetId = set.Id,
                EventText = $"{displayName} logged {set.Weight}kg x {set.Reps} (e1RM: {e1Rm:F1}kg)",
                IsPr = isNewPeak
            });
        }

        if (isChallenger)
        {
            duel.ChallengerBaselineValue = baseline;
            duel.ChallengerCurrentValue = currBest;
        }
        else
        {
            duel.RivalBaselineValue = baseline;
            duel.RivalCurrentValue = currBest;
        }
    }
}
