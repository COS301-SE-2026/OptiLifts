using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutsHandler : IRequestHandler<GetWorkoutsQuery, IReadOnlyList<WorkoutCardDto>>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutsHandler(OptiLiftsDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WorkoutCardDto>> Handle(GetWorkoutsQuery request, CancellationToken cancellationToken)
    {
        var workouts = await _dbContext.Workouts
            .AsNoTracking()
            .Where(workout => workout.CreatedBy == request.UserId)
            .OrderByDescending(workout => workout.CreatedAt)
            .Select(workout => new {
                workout.Id,
                workout.Name,
                workout.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (workouts.Count == 0)
        {
            return Array.Empty<WorkoutCardDto>();
        }

        var workoutIds = workouts.Select(workout => workout.Id).ToArray();

        var workoutExercises = await (
                from set in _dbContext.Sets.AsNoTracking()
                join exercise in _dbContext.Exercises.AsNoTracking() on set.ExerciseId equals exercise.Id
                where workoutIds.Contains(set.WorkoutId)
                select new {
                    set.WorkoutId,
                    set.OrderIndex,
                    exercise.Id,
                    ExerciseName = exercise.Name,
                    exercise.PrimaryMuscles
                })
            .ToListAsync(cancellationToken);

        return workouts.Select(workout =>
        {
            var entries = workoutExercises
                .Where(entry => entry.WorkoutId == workout.Id)
                .OrderBy(entry => entry.OrderIndex)
                .ToList();
            var exerciseCount = entries
                .Select(entry => entry.Id)
                .Distinct()
                .Count();
            var exercisePreview = entries
                .Select(entry => entry.ExerciseName)
                .Distinct()
                .Take(3)
                .ToArray();
            var primaryMuscleGroups = entries
                .SelectMany(entry => entry.PrimaryMuscles)
                .Distinct()
                .ToArray();

            return new WorkoutCardDto(
                workout.Id,
                workout.Name,
                primaryMuscleGroups,
                exerciseCount,
                exercisePreview,
                workout.CreatedAt);
        }).ToList();
    }
}
