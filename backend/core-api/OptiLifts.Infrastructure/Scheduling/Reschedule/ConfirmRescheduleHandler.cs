using System.Reflection.Metadata.Ecma335;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Scheduling.Reschedule;

public class ConfirmRescheduleHandler : IRequestHandler<ConfirmRescheduleCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;
    public ConfirmRescheduleHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ConfirmRescheduleCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return true;
        }
        var entryIds = request.Items.Select(i => i.EntryId).ToList();
        var entries = await _dbContext.ScheduledEntries
        .Where(e => e.UserId == request.UserId && entryIds.Contains(e.Id))
        .ToListAsync(cancellationToken);
        var update = request.Items.ToDictionary(i => i.EntryId, i => i.NewScheduledAt);
        foreach (var entry in entries)
        {
            if (update.TryGetValue(entry.Id, out var nnewDate))
            {
                entry.Scheduled = DateTime.SpecifyKind(nnewDate, DateTimeKind.Utc);
                if (entry.Status == ScheduleStatus.Missed)
                {
                    entry.Status = ScheduleStatus.Scheduled;
                }
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}