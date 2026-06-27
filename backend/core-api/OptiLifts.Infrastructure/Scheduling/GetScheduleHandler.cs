using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class GetScheduleHandler : IRequestHandler<GetScheduleQuery, IReadOnlyList<ScheduledEntryDto>>
{
    private readonly OptiLiftsDbContext _dbContext;
    public GetScheduleHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ScheduledEntryDto>> Handle(GetScheduleQuery request, CancellationToken cancellationToken)
    {
        DateTime start, end;
        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            start = request.StartDate.Value.Date;
            end = request.EndDate.Value.Date;
        }
        else
        {
            var now = DateTime.UtcNow.Date;
            int dif = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            start = now.AddDays(-dif);
            end = start.AddDays(6);
        }

        var entries = await (
            from entry in _dbContext.ScheduledEntries.AsNoTracking()
            where entry.UserId == request.UserId && entry.Scheduled >=start && entry.Scheduled < end.AddDays(1)
            join workout in _dbContext.Workouts.AsNoTracking()
                on entry.WorkoutId equals workout.Id
            orderby entry.Scheduled
            select new ScheduledEntryDto(
                entry.Id,
                entry.WorkoutId,
                workout.Name,
                entry.Scheduled,
                entry.Status.ToString()
            ))
            .ToListAsync(cancellationToken);
        return entries;
    }
}