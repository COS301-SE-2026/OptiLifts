using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Training;

public interface ISeriesBuilder
{
    Task<IReadOnlyList<SeriesPoint>> BuildAsync(Guid userId, Guid exerciseId, DateTime? since, CancellationToken cancellationToken);
}

public sealed record SeriesPoint(
    Guid LogId,
    DateTime Date,
    float E1rm,
    float? AvgRpe,
    float VolumeLoad,
    int SetCount
);

public sealed class SeriesBuilder : ISeriesBuilder
{
    private readonly OptiLiftsDbContext _dbContext;

    private static readonly ExerciseType[] SupportedTypes =
    [
        ExerciseType.WeightReps,
        ExerciseType.BodyweightReps,
        ExerciseType.WeightedBodyweight,
        ExerciseType.AssistedWeightReps
    ];

    public SeriesBuilder(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SeriesPoint>> BuildAsync(Guid userId, Guid exerciseId, DateTime? since, CancellationToken cancellationToken)
    {
        var exer = await _dbContext.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);

        if (exer is null || !SupportedTypes.Contains(exer.ExerciseType))
        {
            return [];
        }

        var bodyweightNeeded = exer.ExerciseType is ExerciseType.BodyweightReps or ExerciseType.WeightedBodyweight or ExerciseType.AssistedWeightReps;

        float currBodyweight = 0;

        if (bodyweightNeeded)
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is not null && double.TryParse(user.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var w))
            {
                currBodyweight = (float)w;
            }
        }

        var query =
            from setLog in _dbContext.WorkoutLogSets.AsNoTracking()
            join workoutLog in _dbContext.WorkoutLogs.AsNoTracking() on setLog.LogId equals workoutLog.Id
            join entry in _dbContext.ScheduledEntries.AsNoTracking() on workoutLog.EntryId equals entry.Id
            where setLog.ExerciseId == exerciseId && entry.UserId == userId
                && workoutLog.CompletedAt != null && setLog.Type == SetType.Normal && setLog.Reps > 0
            select new { setLog, workoutLog.Id, workoutLog.CompletedAt };

        if (since.HasValue)
        {
            query = query.Where(x => x.CompletedAt >= since.Value);
        }

        var rows = await query.ToListAsync(cancellationToken);

        var setsOfWorking = exer.ExerciseType == ExerciseType.WeightReps ? rows.Where(r => r.setLog.Weight > 0).ToList() : rows;

        var compound = string.Equals(exer.Mechanic, "compound", StringComparison.OrdinalIgnoreCase);

        float EffectiveWeight(WorkoutSetLog s) => exer.ExerciseType switch
        {
            ExerciseType.WeightReps => s.Weight,
            ExerciseType.BodyweightReps => currBodyweight,
            ExerciseType.WeightedBodyweight => currBodyweight + s.Weight,
            ExerciseType.AssistedWeightReps => MathF.Max(0f, currBodyweight - s.Weight),
            _ => s.Weight
        };

        var pts = setsOfWorking.GroupBy(r => r.Id).Select(sessionGroup =>
            {
                var sets = sessionGroup.ToList();

                var bestE1rm = sets.Max(r => ComputeE1rm(EffectiveWeight(r.setLog), r.setLog.Reps, compound));

                var loggedRpes = sets
                    .Where(r => r.setLog.Rpe.HasValue && r.setLog.Rpe.Value > 0)
                    .Select(r => r.setLog.Rpe!.Value).ToList();

                var volLoad = sets.Sum(r => EffectiveWeight(r.setLog) * r.setLog.Reps);

                return new SeriesPoint(
                    sessionGroup.Key,
                    sets[0].CompletedAt!.Value,
                    bestE1rm,
                    loggedRpes.Count > 0 ? loggedRpes.Average() : null,
                    volLoad,
                    sets.Count);
            })
            .OrderBy(p => p.Date).ToList();

        return pts;
    }

    private static float ComputeE1rm(float weight, int reps, bool isCompound)
    {
        if (reps <= 1)
        {
            return weight;
        }

        return isCompound ? 100f * weight / (52.2f + 41.9f * MathF.Exp(-0.055f * reps)) : weight * (1f + reps / 30f);
    }
}
