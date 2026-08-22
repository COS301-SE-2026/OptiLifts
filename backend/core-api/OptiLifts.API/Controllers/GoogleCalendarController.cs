using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Google;
using OptiLifts.Application.Auth.Login;
using OptiLifts.Application.Auth.Logout;
using OptiLifts.Application.Auth.Me;
using OptiLifts.Application.Auth.Refresh;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Migrations;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/users/me/google-calendar")]
[Authorize]
public sealed class GoogleCalendarController : ControllerBase
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IGoogleCalendarService _calendarService;

    public GoogleCalendarController(OptiLiftsDbContext dbContext, IGoogleCalendarService calendarService)
    {
        _dbContext = dbContext;
        _calendarService = calendarService;
    }
    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

    public sealed record CalendarSettingsResponse(bool isConnected, bool SyncEnabled);
    [HttpGet("settings")]
    public async Task<ActionResult<CalendarSettingsResponse>> GetSettings(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u=> u.Id == userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }
        bool connected = !string.IsNullOrWhiteSpace(user.GoogleCalendarRefreshToken);
        return Ok(new CalendarSettingsResponse(connected, user.GoogleCalendarSyncEnabled));

    }

    public sealed record ConnectCalendarRequest(string Code, string RedirectUri);
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectCalendar(
        [FromBody] ConnectCalendarRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=> u.Id == userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        var refreshToken = await _calendarService.ExchangeCodeForRefreshTokenAsync(request.Code, request.RedirectUri, cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return BadRequest(new
            {
                message = "Failed to get authorisation from google calendar"
            });
        }

        user.GoogleCalendarRefreshToken = refreshToken;
        user.GoogleCalendarSyncEnabled = true;
        var calendarId = await _calendarService.GetOrCreateOptiLiftsCalendarIdAsync(refreshToken, cancellationToken);
        user.GoogleCalendarId = calendarId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncFutureWorkoutsForUserAsync(user, cancellationToken);

        return Ok(new
        {
            connected = true,
            syncEnabled = true
        });
    }

    public sealed record ToggleSyncRequest(bool Enabled);
    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleSync(
        [FromBody] ToggleSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=> u.Id == userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        user.GoogleCalendarSyncEnabled = request.Enabled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.Enabled && !string.IsNullOrWhiteSpace(user.GoogleCalendarRefreshToken))
        {
            await SyncFutureWorkoutsForUserAsync(user, cancellationToken);
        }
        return Ok(new
        {
            syncEnabled = user.GoogleCalendarSyncEnabled
        });
    }

    private async Task SyncFutureWorkoutsForUserAsync(Domain.Users.User user, CancellationToken cancellationToken){
        if (!user.GoogleCalendarSyncEnabled || string.IsNullOrWhiteSpace(user.GoogleCalendarRefreshToken)) return;

        var now = DateTime.UtcNow;
        var futureentries = await _dbContext.ScheduledEntries
        .Where(e => e.UserId == user.Id && e.Scheduled >= now && string.IsNullOrEmpty(e.GoogleEventId))
        .ToListAsync(cancellationToken);

        if (!futureentries.Any()) return;
        var calendarId = user.GoogleCalendarId;
        if (string.IsNullOrWhiteSpace(calendarId))
        {
            calendarId = await _calendarService.GetOrCreateOptiLiftsCalendarIdAsync(user.GoogleCalendarRefreshToken, cancellationToken);
            user.GoogleCalendarId = calendarId;
        }

        foreach(var entry in futureentries)
        {
            var workout = await _dbContext.Workouts.AsNoTracking().FirstOrDefaultAsync(w => w.Id == entry.WorkoutId, cancellationToken);
            if (workout == null) continue;

            var exercises = await (
                from we in _dbContext.WorkoutExercises.AsNoTracking()
                where we.WorkoutId == workout.Id
                join ex in _dbContext.Exercises.AsNoTracking() on we.ExerciseId equals ex.Id
                orderby we.OrderIndex
                select new {ex.Name, setCount = _dbContext.Sets.Count(s => s.WorkoutExerciseId == we.Id)}

            ).ToListAsync(cancellationToken);

            var exerciseList = string.Join("\n", exercises.Select(e=> $"- {e.Name} ({e.setCount} sets)"));
            var description = $"Planned Workout Session in OptiLifts\n\nExercises:\n{(string.IsNullOrWhiteSpace(exerciseList) ? "- Custom Exercises": exerciseList)}\n\nOpen in OptiLifts: https://app.optilifts.app/schedule";
            var eventDto = new GoogleCalendarEventDto(
                Summary: $"OptiLifts: {workout.Name}",
                Description: description,
                StartTime: entry.Scheduled,
                DurationMinutes: 60
            );

            var eventId = await _calendarService.CreateEventAsync(user.GoogleCalendarRefreshToken, calendarId, eventDto, cancellationToken);
            if (!string.IsNullOrWhiteSpace(eventId))
            {
                entry.GoogleEventId = eventId;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

}