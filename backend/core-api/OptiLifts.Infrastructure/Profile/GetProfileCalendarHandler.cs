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

        var highlightedDates = await _dbContext.ScheduledEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == request.UserId && entry.Scheduled >= startOfMonth && entry.Scheduled < startOfNextMonth)
            .OrderBy(entry => entry.Scheduled)
            .Select(entry => entry.Scheduled.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Distinct()
            .ToListAsync(cancellationToken);

        return new ProfileCalendarDto(highlightedDates);
    }
}