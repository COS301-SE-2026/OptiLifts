namespace OptiLifts.Application.Auth.Abstractions;

public sealed record GoogleCalendarEventDto(
    string Summary,
    string Description,
    DateTime StartTime,
    int DurationMinutes = 60
);

public interface IGoogleCalendarService
{
    Task<string?> ExchangeCodeForRefreshTokenAsync(string code, string redirectUri, CancellationToken cancellationToken= default);
    Task<string> GetOrCreateOptiLiftsCalendarIdAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task <string> CreateEventAsync(string refreshToken, string calendarId, GoogleCalendarEventDto eventDto, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(string refreshToken, string calendarId, string eventId, CancellationToken cancellationToken = default);
}