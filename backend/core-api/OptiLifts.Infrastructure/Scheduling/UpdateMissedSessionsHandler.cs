using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.UpdateMissedSessions;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class UpdateMissedSessionsHandler : IRequestHandler<UpdateMissedSessionsCommand, UpdateMissedSessionsResult>
{
    private readonly OptiLiftsDbContext _dbContext;
    public UpdateMissedSessionsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UpdateMissedSessionsResult> Handle(UpdateMissedSessionsCommand request, CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;
        var missEntries = await _dbContext.ScheduledEntries
            .Where(entry => entry.UserId == request.UserId &&
            entry.Status == ScheduleStatus.Scheduled &&
            entry.Scheduled < todayStart).ToListAsync(cancellationToken);

        if (missEntries.Count == 0)
        {
            return new UpdateMissedSessionsResult(0);
        }
        foreach(var entry in missEntries)
        {
            entry.Status = ScheduleStatus.Missed;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return new UpdateMissedSessionsResult(missEntries.Count);
    }
}