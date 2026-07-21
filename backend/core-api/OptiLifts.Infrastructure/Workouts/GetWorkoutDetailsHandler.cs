using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutDetails;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutDetailsHandler : IRequestHandler<GetWorkoutDetailsQuery, WorkoutDetailDto?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutDetailsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutDetailDto?> Handle(GetWorkoutDetailsQuery request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .AsNoTracking()
            .Where(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId)
            .Select(w => new { w.Id, w.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (workout is null)
        {
            return null;
        }

        var exerciseRows = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == request.WorkoutId
            join exercise in _dbContext.Exercises.AsNoTracking()
                on workoutExercise.ExerciseId equals exercise.Id
            join muscle in _dbContext.Muscles.AsNoTracking()
                on exercise.PrimaryMuscleId equals muscle.Id into muscleJoin
            from muscle in muscleJoin.DefaultIfEmpty()
            orderby workoutExercise.OrderIndex
            select new
            {
                workoutExercise.Id,
                workoutExercise.ExerciseId,
                ExerciseName = exercise.Name,
                MuscleGroup = muscle != null ? muscle.Name : "Unknown",
                workoutExercise.OrderIndex
            })
            .ToListAsync(cancellationToken);

        var workoutExerciseIds = exerciseRows.Select(row => row.Id).ToArray();

        var setRows = workoutExerciseIds.Length == 0
            ? []
            : await _dbContext.Sets
                .AsNoTracking()
                .Where(set => workoutExerciseIds.Contains(set.WorkoutExerciseId))
                .Select(set => new
                {
                    set.Id,
                    set.WorkoutExerciseId,
                    Type = set.Type.ToString(),
                    set.Reps,
                    set.Weight,
                    set.Duration,
                    set.Distance,
                    set.OrderIndex,
                    set.RestTime
                })
                .ToListAsync(cancellationToken);

        var setsByExerciseId = setRows
            .GroupBy(row => row.WorkoutExerciseId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WorkoutSetDetailDto>)group
                    .OrderBy(row => row.OrderIndex)
                    .Select(row => new WorkoutSetDetailDto(
                        row.Id,
                        row.Type,
                        row.Reps,
                        row.Weight,
                        row.Duration,
                        row.Distance,
                        row.OrderIndex,
                        row.RestTime))
                    .ToList());

        var exercises = exerciseRows
            .Select(row => new WorkoutExerciseDetailDto(
                row.Id,
                row.ExerciseId,
                row.ExerciseName,
                row.MuscleGroup,
                row.OrderIndex,
                setsByExerciseId.TryGetValue(row.Id, out var sets) ? sets : []))
            .ToList();

        var primaryMuscleGroups = exercises
            .Select(exercise => exercise.MuscleGroup)
            .Where(name => !string.IsNullOrWhiteSpace(name) && name != "Unknown")
            .Distinct()
            .Take(3)
            .ToArray();

        return new WorkoutDetailDto(workout.Id, workout.Name, primaryMuscleGroups, exercises);
    }
}
