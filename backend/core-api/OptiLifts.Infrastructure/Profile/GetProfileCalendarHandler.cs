using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Profile;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Profile;

public sealed class GetProfileCalendarHandler : IRequestHandler<GetProfileCalendarQuery, ProfileCalendarDto>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetProfileCalendarHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProfileCalendarDto> Handle(GetProfileCalendarQuery request, CancellationToken cancellationToken)
    {
        var startOfMonth = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfMonth.AddMonths(1);

        var completedLogs = await (
            from log in _dbContext.WorkoutLogs.AsNoTracking()
            where log.EntryId.HasValue && log.CompletedAt != null && log.CompletedAt >= startOfMonth && log.CompletedAt < startOfNextMonth
            join entry in _dbContext.ScheduledEntries.AsNoTracking() on log.EntryId!.Value equals entry.Id
            where entry.UserId == request.UserId
            join workout in _dbContext.Workouts.AsNoTracking() on entry.WorkoutId equals workout.Id
            orderby log.CompletedAt descending
            select new ProfileCalendarEntryRow(
                workout.Id,
                log.Id,
                log.CompletedAt!.Value))
            .ToListAsync(cancellationToken);

        var entries = completedLogs
            .GroupBy(entry => entry.CompletedAt.Date)
            .Select(group => group.First())
            .Select(entry => new ProfileCalendarEntryDto(
                entry.WorkoutId,
                entry.LogId,
                entry.CompletedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ToList();

        return new ProfileCalendarDto(entries);
    }

    private sealed record ProfileCalendarEntryRow(Guid WorkoutId, Guid LogId, DateTime CompletedAt);
}