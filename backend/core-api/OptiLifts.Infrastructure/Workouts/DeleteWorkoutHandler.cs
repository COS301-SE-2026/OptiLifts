using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.DeleteWorkout;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class DeleteWorkoutHandler : IRequestHandler<DeleteWorkoutCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;

    public DeleteWorkoutHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteWorkoutCommand request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId && !w.IsDeleted, cancellationToken);

        if (workout == null)
        {
            return false;
        }

        workout.IsDeleted = true;
        workout.DeletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}