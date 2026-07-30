using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutsHandler : IRequestHandler<GetWorkoutsQuery, IReadOnlyList<WorkoutCardDto>>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WorkoutCardDto>> Handle(GetWorkoutsQuery request, CancellationToken cancellationToken)
    {
        var workouts = await _dbContext.Workouts
            .AsNoTracking()
            .Where(workout => workout.CreatedBy == request.UserId && !workout.IsDeleted)
            .OrderByDescending(workout => workout.CreatedAt)
            .Select(workout => new
            {
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
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutIds.Contains(workoutExercise.WorkoutId)
            join exercise in _dbContext.Exercises.AsNoTracking()
                on workoutExercise.ExerciseId equals exercise.Id
            select new
            {
                workoutExercise.WorkoutId,
                workoutExercise.OrderIndex,
                exercise.Id,
                ExerciseName = exercise.Name,
                exercise.PrimaryMuscleId
            })
            .ToListAsync(cancellationToken);

        var allPrimaryMuscleIds = workoutExercises
            .Select(e => e.PrimaryMuscleId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var muscleMap = new Dictionary<Guid, string>();
        if (allPrimaryMuscleIds.Length > 0)
        {
            muscleMap = await _dbContext.Muscles
                .AsNoTracking()
                .Where(m => allPrimaryMuscleIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);
        }

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
                    .Select(entry => entry.PrimaryMuscleId)
                    .Distinct()
                    .Take(3)
                    .Select(id => muscleMap.TryGetValue(id, out var name) ? name : "")
                    .Where(name => !string.IsNullOrEmpty(name))
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
