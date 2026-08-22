using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Migrations;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class CreateScheduledSessionHandler : IRequestHandler<CreateScheduledSessionCommand, CreateScheduledSessionResult?>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IGoogleCalendarService _calendarService;
    public CreateScheduledSessionHandler(OptiLiftsDbContext dbContext, IGoogleCalendarService calendarService)
    {
        _dbContext = dbContext;
        _calendarService = calendarService;
    }
    public async Task<CreateScheduledSessionResult?> Handle(CreateScheduledSessionCommand request, CancellationToken cancellationToken)
    {
        var workoutExist = await _dbContext.Workouts.AnyAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId && !w.IsDeleted, cancellationToken);
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
                    if (repeattype == "day") currentDate = currentDate.AddDays(interval);
                    else if (repeattype == "week") currentDate = currentDate.AddDays(interval * 7);
                    else if (repeattype == "month") currentDate = currentDate.AddMonths(interval);

                    if (currentDate.Date <= until.Date)
                    {
                        datesToSchedule.Add(currentDate);
                    }
                    else
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

        await SyncWithGoogleCalendar(request, cancellationToken);

        return new CreateScheduledSessionResult(
            lastEntry.Id,
            lastEntry.WorkoutId,
            lastEntry.Scheduled,
            lastEntry.Status
        );
    }

    private async Task SyncWithGoogleCalendar(CreateScheduledSessionCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null || !user.GoogleCalendarSyncEnabled || string.IsNullOrWhiteSpace(user.GoogleCalendarRefreshToken))
        {
            return;
        }

        var workout = await _dbContext.Workouts.AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.WorkoutId, cancellationToken);

        if (workout != null)
        {
            var calendarId = user.GoogleCalendarId ?? await _calendarService.GetOrCreateOptiLiftsCalendarIdAsync(user.GoogleCalendarRefreshToken, cancellationToken);
            user.GoogleCalendarId = calendarId;

            var exercises = await (
                from we in _dbContext.WorkoutExercises.AsNoTracking()
                where we.WorkoutId == workout.Id
                join ex in _dbContext.Exercises.AsNoTracking() on we.ExerciseId equals ex.Id
                orderby we.OrderIndex
                select new { ex.Name, setCount = _dbContext.Sets.Count(s => s.WorkoutExerciseId == we.Id) }

            ).ToListAsync(cancellationToken);

            var exerciseList = string.Join("\n", exercises.Select(e => $"- {e.Name} ({e.setCount} sets)"));
            var description = $"Planned Workout Session in OptiLifts\n\nExercises:\n{(string.IsNullOrWhiteSpace(exerciseList) ? "- Custom Exercises" : exerciseList)}\n\nOpen in OptiLifts: https://app.optilifts.app/schedule";

            foreach (var entry in _dbContext.ScheduledEntries.Local.Where(e => e.UserId == request.UserId && e.Scheduled >= DateTime.UtcNow))
            {
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
}
