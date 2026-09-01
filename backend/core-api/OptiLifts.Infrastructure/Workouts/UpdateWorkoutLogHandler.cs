using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.UpdateWorkoutLog;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class UpdateWorkoutLogHandler : IRequestHandler<UpdateWorkoutLogCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IPlateauDetectionService _plateauDetectionService;

    public UpdateWorkoutLogHandler(OptiLiftsDbContext dbContext, IPlateauDetectionService plateauDetectionService)
    {
        _dbContext = dbContext;
        _plateauDetectionService = plateauDetectionService;
    }

    public async Task<bool> Handle(UpdateWorkoutLogCommand request, CancellationToken cancellationToken)
    {
        var log = await (
            from workoutLog in _dbContext.WorkoutLogs
            join entry in _dbContext.ScheduledEntries on workoutLog.EntryId equals entry.Id
            where workoutLog.Id == request.LogId
                && entry.WorkoutId == request.WorkoutId
                && entry.UserId == request.UserId
            select workoutLog
        ).FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            return false;
        }

        if (request.StartedAt.HasValue)
        {
            log.StartedAt = request.StartedAt.Value;
        }

        if (request.CompletedAt.HasValue)
        {
            log.CompletedAt = request.CompletedAt.Value;
        }

        if (request.Notes is not null)
        {
            log.Notes = request.Notes;
        }

        var oldSetIds = await _dbContext.WorkoutLogSets
            .Where(s => s.LogId == log.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (oldSetIds.Count > 0)
        {
            var oldPrs = await _dbContext.ExercisePrs
                .Where(pr => oldSetIds.Contains(pr.WorkoutLogSetId))
                .ToListAsync(cancellationToken);
            _dbContext.ExercisePrs.RemoveRange(oldPrs);

            var oldSets = await _dbContext.WorkoutLogSets
                .Where(s => s.LogId == log.Id)
                .ToListAsync(cancellationToken);
            _dbContext.WorkoutLogSets.RemoveRange(oldSets);
        }

        var oldLogExercises = await _dbContext.WorkoutLogExercises
            .Where(e => e.LogId == log.Id)
            .ToListAsync(cancellationToken);
        var oldExerciseIds = oldLogExercises.Select(e => e.ExerciseId).ToArray();
        _dbContext.WorkoutLogExercises.RemoveRange(oldLogExercises);

        var orderedExercises = request.Exercises
            .OrderBy(exercise => exercise.OrderIndex)
            .ThenBy(exercise => exercise.ExerciseId)
            .ToArray();

        var currentBestValues = await LoadCurrentBestValuesAsync(
            request.UserId,
            orderedExercises.Select(exercise => exercise.ExerciseId).Distinct().ToArray(),
            cancellationToken);

        foreach (var exercise in orderedExercises)
        {
            _dbContext.WorkoutLogExercises.Add(new WorkoutLogExercise
            {
                LogId = log.Id,
                ExerciseId = exercise.ExerciseId,
                WorkoutExerciseId = exercise.WorkoutExerciseId,
                OrderIndex = exercise.OrderIndex,
                GroupNumber = exercise.GroupNumber
            });

            foreach (var set in exercise.Sets)
            {
                var loggedSet = new WorkoutSetLog
                {
                    LogId = log.Id,
                    ExerciseId = exercise.ExerciseId,
                    WorkoutExerciseId = exercise.WorkoutExerciseId,
                    SetId = set.SetId,
                    Type = ParseSetType(set.Type),
                    Reps = set.Reps,
                    Weight = set.Weight,
                    GroupNumber = set.GroupNumber,
                    Rpe = set.Rpe,
                    Duration = set.Duration,
                    Distance = set.Distance,
                    RestTime = set.RestTime,
                    OrderIndex = set.OrderIndex,
                    LoggedAt = log.CompletedAt ?? DateTime.UtcNow,
                    AiSuggested = false
                };

                _dbContext.WorkoutLogSets.Add(loggedSet);

                MaybeAddExercisePr(
                    request.UserId,
                    exercise.ExerciseId,
                    loggedSet,
                    currentBestValues);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var affectedExerciseIds = oldExerciseIds.Concat(orderedExercises.Select(e => e.ExerciseId)).Distinct();
        foreach (var exerciseId in affectedExerciseIds)
        {
            await _plateauDetectionService.DetectAsync(request.UserId, exerciseId, cancellationToken);
        }
        return true;
    }

    private async Task<Dictionary<(Guid ExerciseId, ExercisePrType PrType), float>> LoadCurrentBestValuesAsync(
        Guid userId,
        Guid[] exerciseIds,
        CancellationToken cancellationToken)
    {
        if (exerciseIds.Length == 0)
        {
            return new Dictionary<(Guid ExerciseId, ExercisePrType PrType), float>();
        }

        var currentBestValues = await _dbContext.ExercisePrs
            .AsNoTracking()
            .Where(pr => pr.UserId == userId && exerciseIds.Contains(pr.ExerciseId))
            .GroupBy(pr => new { pr.ExerciseId, pr.PrType })
            .Select(group => new
            {
                group.Key.ExerciseId,
                group.Key.PrType,
                BestValue = group.Max(pr => pr.PrValue)
            })
            .ToListAsync(cancellationToken);

        return currentBestValues.ToDictionary(item => (item.ExerciseId, item.PrType), item => item.BestValue);
    }

    private void MaybeAddExercisePr(
        Guid userId,
        Guid exerciseId,
        WorkoutSetLog loggedSet,
        Dictionary<(Guid ExerciseId, ExercisePrType PrType), float> currentBestValues)
    {
        if (loggedSet.Type != SetType.Normal || loggedSet.Weight <= 0 || loggedSet.Reps <= 0)
        {
            return;
        }

        var weightKey = (exerciseId, ExercisePrType.MaxWeight);
        var weightBest = currentBestValues.TryGetValue(weightKey, out var existingWeightBest) ? existingWeightBest : float.MinValue;
        if (loggedSet.Weight > weightBest)
        {
            _dbContext.ExercisePrs.Add(new ExercisePr
            {
                UserId = userId,
                ExerciseId = exerciseId,
                WorkoutLogSetId = loggedSet.Id,
                PrType = ExercisePrType.MaxWeight,
                PrValue = loggedSet.Weight,
                AchievedWeight = loggedSet.Weight,
                AchievedReps = loggedSet.Reps
            });

            currentBestValues[weightKey] = loggedSet.Weight;
        }

        var volumeKey = (exerciseId, ExercisePrType.MaxSetVolume);
        var candidateVolume = loggedSet.Weight * loggedSet.Reps;
        var volumeBest = currentBestValues.TryGetValue(volumeKey, out var existingVolumeBest) ? existingVolumeBest : float.MinValue;
        if (candidateVolume > volumeBest)
        {
            _dbContext.ExercisePrs.Add(new ExercisePr
            {
                UserId = userId,
                ExerciseId = exerciseId,
                WorkoutLogSetId = loggedSet.Id,
                PrType = ExercisePrType.MaxSetVolume,
                PrValue = candidateVolume,
                AchievedWeight = loggedSet.Weight,
                AchievedReps = loggedSet.Reps
            });

            currentBestValues[volumeKey] = candidateVolume;
        }
    }

    private static SetType ParseSetType(string value) => Enum.TryParse<SetType>(value, ignoreCase: true, out var parsed) ? parsed : SetType.Normal;
}
