using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutDetailHandler : IRequestHandler<GetWorkoutDetailQuery, WorkoutDetailDto?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutDetailHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutDetailDto?> Handle(GetWorkoutDetailQuery request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .AsNoTracking()
            .FirstOrDefaultAsync(workout => workout.Id == request.WorkoutId && workout.CreatedBy == request.UserId && !workout.IsDeleted, cancellationToken);

        if (workout is null)
        {
            return null;
        }

        var workoutExercises = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == workout.Id
            join exercise in _dbContext.Exercises.AsNoTracking()
                on workoutExercise.ExerciseId equals exercise.Id
            join muscle in _dbContext.Muscles.AsNoTracking()
                on exercise.PrimaryMuscleId equals muscle.Id
            join eg in _dbContext.ExerciseGroups.AsNoTracking()
                on workoutExercise.GroupId equals eg.Id into egJoin
            from exerciseGroup in egJoin.DefaultIfEmpty()
            orderby workoutExercise.OrderIndex, exercise.Name
            select new
            {
                workoutExercise.Id,
                workoutExercise.ExerciseId,
                workoutExercise.OrderIndex,
                workoutExercise.GroupId,
                ExerciseName = exercise.Name,
                PrimaryMuscleName = muscle.Name,
                ExerciseType = exercise.ExerciseType,
                GroupType = exerciseGroup != null ? exerciseGroup.Type.ToString() : null,
                GroupRestTime = (int?)(exerciseGroup != null ? exerciseGroup.RestTime : null),
                ImageUrl = exercise.ImageUrl
            })
            .ToListAsync(cancellationToken);

        var workoutExerciseIds = workoutExercises.Select(entry => entry.Id).ToArray();
        var setsByWorkoutExerciseId = new Dictionary<Guid, List<WorkoutSetDto>>();

        if (workoutExerciseIds.Length > 0)
        {
            var workoutSets = await _dbContext.Sets
                .AsNoTracking()
                .Where(workoutSet => workoutExerciseIds.Contains(workoutSet.WorkoutExerciseId))
                .OrderBy(workoutSet => workoutSet.OrderIndex)
                .Select(workoutSet => new
                {
                    workoutSet.Id,
                    workoutSet.WorkoutExerciseId,
                    workoutSet.Type,
                    workoutSet.Reps,
                    workoutSet.Weight,
                    workoutSet.Duration,
                    workoutSet.Distance,
                    workoutSet.OrderIndex,
                    workoutSet.RestTime
                })
                .ToListAsync(cancellationToken);

            foreach (var workoutSet in workoutSets)
            {
                if (!setsByWorkoutExerciseId.TryGetValue(workoutSet.WorkoutExerciseId, out var exerciseSets))
                {
                    exerciseSets = [];
                    setsByWorkoutExerciseId[workoutSet.WorkoutExerciseId] = exerciseSets;
                }

                exerciseSets.Add(new WorkoutSetDto(
                    workoutSet.Id,
                    workoutSet.Type.ToString(),
                    workoutSet.Reps,
                    workoutSet.Weight,
                    workoutSet.Duration,
                    workoutSet.Distance,
                    workoutSet.OrderIndex,
                    workoutSet.RestTime));
            }
        }

        var exercises = workoutExercises.Select(entry => new WorkoutExerciseDetailDto(
            entry.Id,
            entry.ExerciseId,
            entry.ExerciseName,
            entry.PrimaryMuscleName,
            ToFrontendExerciseType(entry.ExerciseType),
            entry.OrderIndex,
            setsByWorkoutExerciseId.TryGetValue(entry.Id, out var workoutSets)
                ? workoutSets.ToArray()
                : [],
            entry.GroupId,
            entry.GroupType,
            entry.GroupRestTime,
            entry.ImageUrl)).ToArray();

        var primaryMuscleGroups = exercises
            .Select(exercise => exercise.PrimaryMuscle)
            .Distinct()
            .ToArray();

        var exercisePreview = exercises
            .Select(exercise => exercise.Name)
            .Distinct()
            .Take(3)
            .ToArray();

        return new WorkoutDetailDto(
            workout.Id,
            workout.Name,
            workout.FolderId,
            null,
            workout.CreatedAt,
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
}