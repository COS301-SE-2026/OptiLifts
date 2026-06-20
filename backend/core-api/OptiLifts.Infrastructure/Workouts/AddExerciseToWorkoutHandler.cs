using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.AddExerciseToWorkout;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class AddExerciseToWorkoutHandler : IRequestHandler<AddExerciseToWorkoutCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;

    public AddExerciseToWorkoutHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(AddExerciseToWorkoutCommand request, CancellationToken cancellationToken)
    {
        var workoutExists = await _dbContext.Workouts
            .AsNoTracking()
            .AnyAsync(workout => workout.Id == request.WorkoutId && workout.CreatedBy == request.UserId, cancellationToken);

        if (!workoutExists)
        {
            return false;
        }

        var exerciseExists = await _dbContext.Exercises
            .AsNoTracking()
            .AnyAsync(exercise => exercise.Id == request.ExerciseId, cancellationToken);

        if (!exerciseExists)
        {
            return false;
        }

        var currentMaxOrderIndex = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(we => we.WorkoutId == request.WorkoutId)
            .Select(we => (int?)we.OrderIndex)
            .MaxAsync(cancellationToken) ?? 0;

        var workoutExercise = new WorkoutExercise
        {
            WorkoutId = request.WorkoutId,
            ExerciseId = request.ExerciseId,
            OrderIndex = currentMaxOrderIndex + 1
        };

        _dbContext.WorkoutExercises.Add(workoutExercise);

        var workoutSet = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            Type = SetType.Normal,
            Reps = 0,
            Weight = 0,
            OrderIndex = 1,
            RestTime = 0
        };

        _dbContext.Sets.Add(workoutSet);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}