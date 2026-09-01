using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.ProgressiveOverload;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class CreateWorkoutLogHandler : IRequestHandler<CreateWorkoutLogCom, CreateWorkoutLogRes?>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly ISender? _sender;

    public CreateWorkoutLogHandler(OptiLiftsDbContext dbContext, ISender? sender = null)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task<CreateWorkoutLogRes?> Handle(CreateWorkoutLogCom request, CancellationToken cancellationToken)
    {
        var existingWorkout = await _dbContext.Workouts.AnyAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId && !w.IsDeleted, cancellationToken);

        if (!existingWorkout)
        {
            return null;
        }

        var existing = await _dbContext.WorkoutLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == request.LogId, cancellationToken);

        if (existing is not null)
        {
            return new CreateWorkoutLogRes(existing.Id, existing.EntryId ?? Guid.Empty, AlreadyExisted: true);
        }

        Guid entryId;

        if (request.EntryId is Guid providedEntryId)
        {
            var valid = await _dbContext.ScheduledEntries
                .FirstOrDefaultAsync(e => e.Id == providedEntryId && e.UserId == request.UserId && e.WorkoutId == request.WorkoutId, cancellationToken);

            if (valid is null)
            {
                return null;
            }

            valid.Status = ScheduleStatus.Completed;
            entryId = valid.Id;
        }
        else
        {
            var startedDay = DateTime.SpecifyKind(request.StartedAt.Date, DateTimeKind.Utc);

            var plannedEnt = await _dbContext.ScheduledEntries
                .Where(e => e.UserId == request.UserId
                    && e.WorkoutId == request.WorkoutId && e.Status != ScheduleStatus.Completed
                    && e.Scheduled >= startedDay && e.Scheduled < startedDay.AddDays(1))
                .OrderBy(e => e.Scheduled)
                .FirstOrDefaultAsync(cancellationToken);

            if (plannedEnt is not null)
            {
                plannedEnt.Status = ScheduleStatus.Completed;
                entryId = plannedEnt.Id;
            }
            else
            {
                var entry = new ScheduledEntry
                {
                    WorkoutId = request.WorkoutId,
                    UserId = request.UserId,
                    Scheduled = request.StartedAt,
                    Status = ScheduleStatus.Completed
                };

                _dbContext.ScheduledEntries.Add(entry);
                entryId = entry.Id;
            }
        }

        var log = new WorkoutLog
        {
            Id = request.LogId,
            EntryId = entryId,
            Notes = request.Notes,
            AiModified = false,
            StartedAt = request.StartedAt,
            CompletedAt = request.CompletedAt,
        };
        _dbContext.WorkoutLogs.Add(log);

        var orderedExercises = request.Exercises
            .OrderBy(exercise => exercise.OrderIndex)
            .ThenBy(exercise => exercise.ExerciseId)
            .ToArray();

        var currentBestValues = await LoadCurrentBestValuesAsync(request.UserId, orderedExercises.Select(exercise => exercise.ExerciseId).Distinct().ToArray(), cancellationToken);

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
                    LoggedAt = request.CompletedAt,
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
        await GenerateOverloadAsync(request.UserId, log.CompletedAt.HasValue, orderedExercises.Select(exercise => exercise.ExerciseId), cancellationToken);

        return new CreateWorkoutLogRes(log.Id, entryId, AlreadyExisted: false);
    }

    private async Task GenerateOverloadAsync(Guid userId, bool isCompleted, IEnumerable<Guid> exerciseIds, CancellationToken cancellationToken)
    {
        if (!isCompleted || _sender is null)
        {
            return;
        }

        foreach (var exerciseId in exerciseIds.Distinct())
        {
            await _sender.Send(new GenerateOverloadCommand(userId, exerciseId), cancellationToken);
        }
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
