using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Scheduling.DeleteScheduledSession;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class DeleteScheduledSessionHandler : IRequestHandler<DeleteScheduledSessionCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IGoogleCalendarService _calendarService;
    public DeleteScheduledSessionHandler(OptiLiftsDbContext dbContext, IGoogleCalendarService calendarService)
    {
        _dbContext = dbContext;
        _calendarService = calendarService;
    }
    public async Task<bool> Handle(DeleteScheduledSessionCommand request, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.ScheduledEntries.FirstOrDefaultAsync(e => e.Id == request.SessionId && e.UserId == request.UserId, cancellationToken);
        if (entry == null)
        {
            return false; //does not exist or doesnt belong to user
        }

        if (!string.IsNullOrEmpty(entry.GoogleEventId))
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user != null && user.GoogleCalendarSyncEnabled && !string.IsNullOrWhiteSpace(user.GoogleCalendarRefreshToken) && !string.IsNullOrWhiteSpace(user.GoogleCalendarId))
            {
                try
                {
                    await _calendarService.DeleteEventAsync(user.GoogleCalendarRefreshToken, user.GoogleCalendarId, entry.GoogleEventId, cancellationToken);
                }
                catch
                {
                    //ignore google calendar errors
                }

            }
        }

        _dbContext.ScheduledEntries.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}