using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.UpdateScheduledSessionStatus;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class UpdateScheduledSessionStatusHandler : IRequestHandler<UpdateScheduledSessionStatusCommand, UpdateScheduledSessionStatusResult?>
{
    private readonly OptiLiftsDbContext _dbContext;
    public UpdateScheduledSessionStatusHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<UpdateScheduledSessionStatusResult?> Handle(UpdateScheduledSessionStatusCommand request, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.ScheduledEntries.FirstOrDefaultAsync(e=> e.Id == request.SessionId && e.UserId == request.UserId, cancellationToken);
        if (entry == null)
        {
            return null; //does not exist or doesnt belong to user
        }

        entry.Status = request.Status;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new UpdateScheduledSessionStatusResult(
            entry.Id,
            entry.WorkoutId,
            entry.Scheduled,
            entry.Status
        );

    }
}