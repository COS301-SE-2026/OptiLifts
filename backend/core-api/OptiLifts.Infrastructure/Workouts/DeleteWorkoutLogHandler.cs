using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.DeleteWorkoutLog;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class DeleteWorkoutLogHandler : IRequestHandler<DeleteWorkoutLogCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;

    public DeleteWorkoutLogHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteWorkoutLogCommand request, CancellationToken cancellationToken)
    {
        var log = await (
            from workoutLog in _dbContext.WorkoutLogs
            join entry in _dbContext.ScheduledEntries on workoutLog.EntryId equals entry.Id
            where workoutLog.Id == request.LogId
                && entry.WorkoutId == request.WorkoutId
                && entry.UserId == request.UserId
            select workoutLog
        ).FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            return false;
        }

        _dbContext.WorkoutLogs.Remove(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}