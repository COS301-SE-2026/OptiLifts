using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.ReplaceWorkoutExercise;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class ReplaceWorkoutExerciseHandler : IRequestHandler<ReplaceWorkoutExerciseCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;

    public ReplaceWorkoutExerciseHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ReplaceWorkoutExerciseCommand request, CancellationToken cancellationToken)
    {
        var workoutExsts = await _dbContext.Workouts
            .AsNoTracking().AnyAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId && !w.IsDeleted, cancellationToken);

        if (!workoutExsts)
        {
            return false;
        }

        var newExerExsts = await _dbContext.Exercises
            .AsNoTracking().AnyAsync(e => e.Id == request.NewExerciseId, cancellationToken);

        if (!newExerExsts)
        {
            return false;
        }

        var matchingWorkoutExers = await _dbContext.WorkoutExercises
            .Where(we => we.WorkoutId == request.WorkoutId && we.ExerciseId == request.OldExerciseId).ToListAsync(cancellationToken);

        if (matchingWorkoutExers.Count == 0)
        {
            return false;
        }

        foreach (var workoutExercise in matchingWorkoutExers)
        {
            workoutExercise.ExerciseId = request.NewExerciseId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
