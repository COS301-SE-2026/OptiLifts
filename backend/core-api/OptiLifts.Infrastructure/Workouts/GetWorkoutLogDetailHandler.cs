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
                ExerciseType = exercise.ExerciseType,
                ImageUrl = exercise.ImageUrl
            })
            .ToListAsync(cancellationToken);

        var secondaryMuscleRows = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == workout.Id
            join secondary in _dbContext.SecMuscles.AsNoTracking()
                on workoutExercise.ExerciseId equals secondary.ExerciseId
            join muscle in _dbContext.Muscles.AsNoTracking()
                on secondary.MuscleId equals muscle.Id
            select new
            {
                workoutExercise.ExerciseId,
                muscle.Name
            })
            .ToListAsync(cancellationToken);

        var secondaryMusclesByExerciseId = secondaryMuscleRows
            .GroupBy(entry => entry.ExerciseId)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Name).Distinct().ToArray());

        var logExercises = await (
            from workoutLogExercise in _dbContext.WorkoutLogExercises.AsNoTracking()
            where workoutLogExercise.LogId == log.Id
            join exercise in _dbContext.Exercises.AsNoTracking() on workoutLogExercise.ExerciseId equals exercise.Id
            join muscle in _dbContext.Muscles.AsNoTracking() on exercise.PrimaryMuscleId equals muscle.Id
            orderby workoutLogExercise.OrderIndex, workoutLogExercise.Id
            select new WorkoutLogExerciseRow(
                workoutLogExercise.Id,
                workoutLogExercise.ExerciseId,
                workoutLogExercise.WorkoutExerciseId,
                workoutLogExercise.OrderIndex,
                exercise.Name,
                muscle.Name,
                exercise.ExerciseType,
                exercise.ImageUrl
                ))
            .ToListAsync(cancellationToken);

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
                workoutSetLog.Duration,
                workoutSetLog.Distance,
                workoutSetLog.RestTime,
                workoutSetLog.GroupNumber,
                workoutSetLog.Rpe))
            .ToListAsync(cancellationToken);

        ExerciseProjection[] exercises;

        if (logExercises.Count > 0)
        {
            var orderedExercises = logExercises
                .Select(row => new ExerciseProjection(
                    row.WorkoutExerciseId ?? row.Id,
                    row.ExerciseId,
                    row.ExerciseName,
                    row.PrimaryMuscleName,
                    row.ExerciseType,
                    row.ImageUrl,
                    row.OrderIndex))
                .OrderBy(row => row.OrderIndex)
                .ThenBy(row => row.Name)
                .ToArray();

            var exerciseKeyByWorkoutExerciseId = logExercises
                .Where(row => row.WorkoutExerciseId is not null)
                .ToDictionary(row => row.WorkoutExerciseId!.Value, row => row.WorkoutExerciseId!.Value);

            var exercisesByExerciseId = orderedExercises
                .GroupBy(exercise => exercise.ExerciseId)
                .ToDictionary(group => group.Key, group => group.ToArray());

            var setsByExerciseKey = ResolveSets(logSetRows, exerciseKeyByWorkoutExerciseId, exercisesByExerciseId);

            exercises = orderedExercises
                .Select(entry => entry with
                {
                    Sets = setsByExerciseKey.TryGetValue(entry.Id, out var workoutSets)
                        ? workoutSets
                        : []
                })
                .ToArray();
        }
        else
        {
            var workoutExerciseById = workoutExercises.ToDictionary(exercise => exercise.Id);

            var exerciseKeyByWorkoutExerciseId = workoutExerciseById
                .ToDictionary(entry => entry.Key, entry => entry.Key);

            var orderedExercises = workoutExercises
                .Select(entry => new ExerciseProjection(
                    entry.Id,
                    entry.ExerciseId,
                    entry.ExerciseName,
                    entry.PrimaryMuscleName,
                    entry.ExerciseType,
                    entry.ImageUrl,
                    entry.OrderIndex))
                .ToArray();

            var exercisesByExerciseId = orderedExercises
                .GroupBy(exercise => exercise.ExerciseId)
                .ToDictionary(group => group.Key, group => group.ToArray());

            var setsByExerciseKey = ResolveSets(logSetRows, exerciseKeyByWorkoutExerciseId, exercisesByExerciseId);

            exercises = orderedExercises
                .Select(entry => entry with
                {
                    Sets = setsByExerciseKey.TryGetValue(entry.Id, out var workoutSets)
                        ? workoutSets
                        : []
                })
                .ToArray();
        }

        var exerciseDtos = exercises.Select(entry => new WorkoutLogExerciseDetailDto(
            entry.Id,
            entry.ExerciseId,
            entry.Name,
            entry.PrimaryMuscle,
            secondaryMusclesByExerciseId.TryGetValue(entry.ExerciseId, out var secondaryMuscles)
                ? secondaryMuscles
                : [],
            ToFrontendExerciseType(entry.ExerciseType),
            entry.OrderIndex,
            entry.ImageUrl,
            entry.Sets)).ToArray();

        var primaryMuscleGroups = exerciseDtos
            .Select(exercise => exercise.PrimaryMuscle)
            .Distinct()
            .ToArray();

        var exercisePreview = exerciseDtos
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
            log.StartedAt,
            log.CompletedAt,
            log.CompletedAt is null ? null : FormatDuration(log.CompletedAt.Value - log.StartedAt),
            primaryMuscleGroups,
            exercisePreview,
            exerciseDtos);
    }

    private static Dictionary<Guid, WorkoutLogSetDto[]> ResolveSets(
        IEnumerable<WorkoutLogSetRow> logSetRows,
        IReadOnlyDictionary<Guid, Guid> exerciseKeyByWorkoutExerciseId,
        IReadOnlyDictionary<Guid, ExerciseProjection[]> exercisesByExerciseId)
    {
        var resolvedLogSetRows = logSetRows
            .Select(logSetRow =>
            {
                if (logSetRow.WorkoutExerciseId is not null
                    && exerciseKeyByWorkoutExerciseId.TryGetValue(logSetRow.WorkoutExerciseId.Value, out var exactExerciseKey))
                {
                    return new WorkoutLogSetResolvedRow(
                        exactExerciseKey,
                        logSetRow.Id,
                        logSetRow.SetId,
                        logSetRow.Type,
                        logSetRow.Reps,
                        logSetRow.Weight,
                        logSetRow.OrderIndex,
                        logSetRow.Duration,
                        logSetRow.Distance,
                        logSetRow.RestTime,
                        logSetRow.GroupNumber,
                        logSetRow.Rpe);
                }

                if (!exercisesByExerciseId.TryGetValue(logSetRow.ExerciseId, out var matchingExercises))
                {
                    return null;
                }

                var fallbackExercise = matchingExercises[0];
                return new WorkoutLogSetResolvedRow(
                    fallbackExercise.Id,
                    logSetRow.Id,
                    logSetRow.SetId,
                    logSetRow.Type,
                    logSetRow.Reps,
                    logSetRow.Weight,
                    logSetRow.OrderIndex,
                    logSetRow.Duration,
                    logSetRow.Distance,
                    logSetRow.RestTime,
                    logSetRow.GroupNumber,
                    logSetRow.Rpe);
            })
            .Where(resolved => resolved is not null)
            .Select(resolved => resolved!)
            .ToList();

        return resolvedLogSetRows
            .GroupBy(row => row.ExerciseKey)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(row => row.OrderIndex)
                    .ThenBy(row => row.Id)
                    .Select(row => new WorkoutLogSetDto(
                        row.Id,
                        row.SetId,
                        row.Type,
                        row.Reps,
                        row.Weight,
                        row.OrderIndex,
                        row.Duration,
                        row.Distance,
                        row.RestTime,
                        row.GroupNumber,
                        row.Rpe ?? 0))
                    .ToArray());
    }

    private static string ToFrontendExerciseType(OptiLifts.Domain.Workouts.ExerciseType exerciseType)
    {
        return exerciseType switch
        {
            OptiLifts.Domain.Workouts.ExerciseType.WeightReps => "WeightReps",
            OptiLifts.Domain.Workouts.ExerciseType.BodyweightReps => "BodyweightReps",
            OptiLifts.Domain.Workouts.ExerciseType.AssistedWeightReps => "AssistedWeightReps",
            OptiLifts.Domain.Workouts.ExerciseType.WeightedBodyweight => "WeightedBodyWeight",
            OptiLifts.Domain.Workouts.ExerciseType.Duration => "Duration",
            OptiLifts.Domain.Workouts.ExerciseType.DurationWeight => "DurationWeight",
            OptiLifts.Domain.Workouts.ExerciseType.DistanceDuration => "DistanceDuration",
            OptiLifts.Domain.Workouts.ExerciseType.WeightDistance => "WeightDistance",
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
        int? Duration,
        float? Distance,
        int RestTime,
        int GroupNumber,
        float? Rpe);

    private sealed record WorkoutLogExerciseRow(
        Guid Id,
        Guid ExerciseId,
        Guid? WorkoutExerciseId,
        int OrderIndex,
        string ExerciseName,
        string PrimaryMuscleName,
        OptiLifts.Domain.Workouts.ExerciseType ExerciseType,
        string? ImageUrl
        );

    private sealed record ExerciseProjection(
        Guid Id,
        Guid ExerciseId,
        string Name,
        string PrimaryMuscle,
        OptiLifts.Domain.Workouts.ExerciseType ExerciseType,
        string? ImageUrl,
        int OrderIndex)
    {
        public WorkoutLogSetDto[] Sets { get; init; } = [];
    }

    private sealed record WorkoutLogSetResolvedRow(
        Guid ExerciseKey,
        Guid Id,
        Guid? SetId,
        string Type,
        int Reps,
        float Weight,
        int OrderIndex,
        int? Duration,
        float? Distance,
        int RestTime,
        int GroupNumber,
        float? Rpe);
}