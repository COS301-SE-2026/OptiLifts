using Azure.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class CreateScheduledSessionHandler : IRequestHandler<CreateScheduledSessionCommand, CreateScheduledSessionResult?>
{
    private readonly OptiLiftsDbContext _dbContext;
    public CreateScheduledSessionHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<CreateScheduledSessionResult?> Handle(CreateScheduledSessionCommand request, CancellationToken cancellationToken)
    {
        var workoutExist = await _dbContext.Workouts.AnyAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId, cancellationToken);
        if (!workoutExist)
        {
            return null;
        }

        //repeat configuration
        var datesToSchedule = new List<DateTime>
        {
            request.ScheduledAt
        };
        if (!string.IsNullOrEmpty(request.Repeat) && request.Interval.HasValue && request.Until.HasValue)
        {
            var repeattype = request.Repeat.ToLowerInvariant();
            var interval = request.Interval.Value; 
            var until = request.Until.Value;
            bool valid = repeattype == "day" || repeattype == "week" || repeattype == "month";
            if (interval > 0 && valid && until <= request.ScheduledAt.AddYears(1))
            {
                var currentDate = request.ScheduledAt;
                while (true)
                {
                    if (repeattype == "day")currentDate = currentDate.AddDays(interval);
                    else if (repeattype == "week") currentDate = currentDate.AddDays(interval*7);
                    else if(repeattype == "month") currentDate = currentDate.AddMonths(interval);

                    if (currentDate.Date <= until.Date)
                    {
                        datesToSchedule.Add(currentDate);
                    } else
                    {
                        break;
                    }
                }
            }

        }

        ScheduledEntry lastEntry = null!;
        foreach (var schedule in datesToSchedule)
        {
            var entry = new ScheduledEntry
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                WorkoutId = request.WorkoutId,
                Scheduled = schedule,
                Status = request.Status ?? ScheduleStatus.Scheduled
            };
            _dbContext.ScheduledEntries.Add(entry);
            lastEntry = entry;
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateScheduledSessionResult(
            lastEntry.Id,
            lastEntry.WorkoutId,
            lastEntry.Scheduled,
            lastEntry.Status
        );
    }
}