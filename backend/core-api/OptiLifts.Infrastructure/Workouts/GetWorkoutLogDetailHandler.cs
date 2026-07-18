using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutLogDetail;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutLogDetailHandler : IRequestHandler<GetWorkoutLogDetailQuery, WorkoutLogDetailDto?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutLogDetailHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutLogDetailDto?> Handle(GetWorkoutLogDetailQuery request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .AsNoTracking()
            .FirstOrDefaultAsync(currentWorkout => currentWorkout.Id == request.WorkoutId && currentWorkout.CreatedBy == request.UserId, cancellationToken);

        if (workout is null)
        {
            return null;
        }

        var log = await (
            from workoutLog in _dbContext.WorkoutLogs.AsNoTracking()
            join entry in _dbContext.ScheduledEntries.AsNoTracking() on workoutLog.EntryId equals entry.Id
            where workoutLog.Id == request.LogId && entry.WorkoutId == workout.Id && entry.UserId == request.UserId
            select new
            {
                workoutLog.Id,
                workoutLog.StartedAt,
                workoutLog.CompletedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            return null;
        }

        var workoutExercises = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == workout.Id
            join exercise in _dbContext.Exercises.AsNoTracking() on workoutExercise.ExerciseId equals exercise.Id
            join muscle in _dbContext.Muscles.AsNoTracking() on exercise.PrimaryMuscleId equals muscle.Id
            orderby workoutExercise.OrderIndex, exercise.Name
            select new
            {
                workoutExercise.Id,
                workoutExercise.ExerciseId,
                workoutExercise.OrderIndex,
                ExerciseName = exercise.Name,
                PrimaryMuscleName = muscle.Name,
                ExerciseType = exercise.ExerciseType
            })
            .ToListAsync(cancellationToken);

        var workoutExerciseById = workoutExercises.ToDictionary(exercise => exercise.Id);
        var workoutExercisesByExerciseId = workoutExercises
            .GroupBy(exercise => exercise.ExerciseId)
            .ToDictionary(group => group.Key, group => group.OrderBy(exercise => exercise.OrderIndex).ToArray());

        var logSetRows = await _dbContext.WorkoutLogSets
            .AsNoTracking()
            .Where(workoutSetLog => workoutSetLog.LogId == log.Id)
            .OrderBy(workoutSetLog => workoutSetLog.OrderIndex)
            .ThenBy(workoutSetLog => workoutSetLog.Id)
            .Select(workoutSetLog => new WorkoutLogSetRow(
                workoutSetLog.WorkoutExerciseId,
                workoutSetLog.ExerciseId,
                workoutSetLog.Id,
                workoutSetLog.SetId,
                workoutSetLog.Type.ToString(),
                workoutSetLog.Reps,
                workoutSetLog.Weight,
                workoutSetLog.OrderIndex,
                workoutSetLog.Rpe))
            .ToListAsync(cancellationToken);

        var resolvedLogSetRows = logSetRows
            .Select(logSetRow =>
            {
                if (logSetRow.WorkoutExerciseId is not null
                    && workoutExerciseById.TryGetValue(logSetRow.WorkoutExerciseId.Value, out var exactExercise))
                {
                    return new WorkoutLogSetResolvedRow(
                        exactExercise.Id,
                        exactExercise.OrderIndex,
                        logSetRow.Id,
                        logSetRow.SetId,
                        logSetRow.Type,
                        logSetRow.Reps,
                        logSetRow.Weight,
                        logSetRow.OrderIndex,
                        logSetRow.Rpe);
                }

                if (!workoutExercisesByExerciseId.TryGetValue(logSetRow.ExerciseId, out var matchingExercises))
                {
                    return null;
                }

                var fallbackExercise = matchingExercises[0];
                return new WorkoutLogSetResolvedRow(
                    fallbackExercise.Id,
                    fallbackExercise.OrderIndex,
                    logSetRow.Id,
                    logSetRow.SetId,
                    logSetRow.Type,
                    logSetRow.Reps,
                    logSetRow.Weight,
                    logSetRow.OrderIndex,
                    logSetRow.Rpe);
            })
            .Where(resolved => resolved is not null)
            .Select(resolved => resolved!)
            .OrderBy(row => row.WorkoutExerciseOrderIndex)
            .ThenBy(row => row.OrderIndex)
            .ThenBy(row => row.Id)
            .ToList();

        var setsByWorkoutExerciseId = resolvedLogSetRows
            .GroupBy(row => row.WorkoutExerciseId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(row => row.OrderIndex)
                    .Select(row => new WorkoutLogSetDto(
                        row.Id,
                        row.SetId,
                        row.Type.ToString(),
                        row.Reps,
                        row.Weight,
                        row.OrderIndex,
                        row.Rpe))
                    .ToArray());

        var exercises = workoutExercises.Select(entry => new WorkoutLogExerciseDetailDto(
            entry.Id,
            entry.ExerciseId,
            entry.ExerciseName,
            entry.PrimaryMuscleName,
            ToFrontendExerciseType(entry.ExerciseType),
            entry.OrderIndex,
            setsByWorkoutExerciseId.TryGetValue(entry.Id, out var workoutSets)
                ? workoutSets
                : [])).ToArray();

        var primaryMuscleGroups = exercises
            .Select(exercise => exercise.PrimaryMuscle)
            .Distinct()
            .ToArray();

        var exercisePreview = exercises
            .Select(exercise => exercise.Name)
            .Distinct()
            .Take(3)
            .ToArray();

        return new WorkoutLogDetailDto(
            workout.Id,
            log.Id,
            workout.Name,
            workout.FolderId,
            null,
            workout.CreatedAt,
            log.CompletedAt,
            log.CompletedAt is null ? null : FormatDuration(log.CompletedAt.Value - log.StartedAt),
            primaryMuscleGroups,
            exercisePreview,
            exercises);
    }

    private static string ToFrontendExerciseType(OptiLifts.Domain.Workouts.ExerciseType exerciseType)
    {
        return exerciseType switch
        {
            OptiLifts.Domain.Workouts.ExerciseType.WeightReps => "weight-reps",
            OptiLifts.Domain.Workouts.ExerciseType.BodyweightReps => "bodyweight-reps",
            OptiLifts.Domain.Workouts.ExerciseType.AssistedWeightReps => "assisted-bodyweight",
            OptiLifts.Domain.Workouts.ExerciseType.WeightedBodyweight => "weighted-bodyweight",
            OptiLifts.Domain.Workouts.ExerciseType.Duration => "duration",
            OptiLifts.Domain.Workouts.ExerciseType.DurationWeight => "duration-weight",
            OptiLifts.Domain.Workouts.ExerciseType.DistanceDuration => "distance-duration",
            OptiLifts.Domain.Workouts.ExerciseType.WeightDistance => "weight-distance",
            _ => exerciseType.ToString()
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = Math.Max(0, (int)Math.Round(duration.TotalMinutes));
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }

    private sealed record WorkoutLogSetRow(
        Guid? WorkoutExerciseId,
        Guid ExerciseId,
        Guid Id,
        Guid? SetId,
        string Type,
        int Reps,
        float Weight,
        int OrderIndex,
        float Rpe);

    private sealed record WorkoutLogSetResolvedRow(
        Guid WorkoutExerciseId,
        int WorkoutExerciseOrderIndex,
        Guid Id,
        Guid? SetId,
        string Type,
        int Reps,
        float Weight,
        int OrderIndex,
        float Rpe);
}