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

        var logSetRows = await (
            from workoutSetLog in _dbContext.WorkoutLogSets.AsNoTracking()
            where workoutSetLog.LogId == log.Id
            join workoutExercise in _dbContext.WorkoutExercises.AsNoTracking() on workoutSetLog.ExerciseId equals workoutExercise.ExerciseId
            where workoutExercise.WorkoutId == workout.Id
            orderby workoutExercise.OrderIndex, workoutSetLog.OrderIndex, workoutSetLog.Id
            select new WorkoutLogSetRow(
                workoutExercise.Id,
                workoutSetLog.Id,
                workoutSetLog.SetId,
                workoutSetLog.Type.ToString(),
                workoutSetLog.Reps,
                workoutSetLog.Weight,
                workoutSetLog.OrderIndex,
                workoutSetLog.Rpe))
            .ToListAsync(cancellationToken);

        var setsByWorkoutExerciseId = logSetRows
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
            workout.DayIndex,
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
        Guid WorkoutExerciseId,
        Guid Id,
        Guid? SetId,
        string Type,
        int Reps,
        float Weight,
        int OrderIndex,
        float Rpe);
}