using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling.Reschedule;
using OptiLifts.Domain.Users;

namespace OptiLifts.Tests.Api.Tests.Schedule;

public sealed class ConfirmRescheduleHandlerTests
{
    private static async Task<OptiLiftsDbContext> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new OptiLiftsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task Handle_ReturnsTrue_WhenItemsListEmpty()
    {
        using var db = await CreateDbContextAsync();
        var handler = new ConfirmRescheduleHandler(db);
        var command = new ConfirmRescheduleCommand(Guid.NewGuid(), new List<ConfirmRescheduleItemDto>());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdatesDatesandResetsMissedStatusToScheduled()
    {
        using var db = await CreateDbContextAsync();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            EmailHash = "testhash",
            PasswordHash = "passwprd",
            DisplayName = "Test User 1"
        };
        db.Users.Add(user);
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Upper Body",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);

        var entry1 = Guid.NewGuid();
        var entry2 = Guid.NewGuid();

        var ogdate = DateTime.UtcNow.Date;
        var newdate1 = ogdate.AddDays(1).AddHours(8);
        var newdate2 = ogdate.AddDays(2).AddHours(10);

        db.ScheduledEntries.Add(new ScheduledEntry
        {
            Id = entry1,
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = ogdate,
            Status = ScheduleStatus.Missed
        });
        db.ScheduledEntries.Add(new ScheduledEntry
        {
            Id = entry2,
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = ogdate,
            Status = ScheduleStatus.Scheduled
        });
        await db.SaveChangesAsync();

        var handler = new ConfirmRescheduleHandler(db);
        var items = new List<ConfirmRescheduleItemDto>
        {
            new ConfirmRescheduleItemDto(entry1, newdate1),
            new ConfirmRescheduleItemDto(entry2, newdate2)
        };
        var command = new ConfirmRescheduleCommand(userId, items);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeTrue();

        var updated1 = await db.ScheduledEntries.FindAsync(entry1);
        updated1.Should().NotBeNull();
        updated1!.Scheduled.Should().Be(DateTime.SpecifyKind(newdate1, DateTimeKind.Utc));
        updated1.Status.Should().Be(ScheduleStatus.Scheduled);
        var updated2 = await db.ScheduledEntries.FindAsync(entry2);
        updated2.Should().NotBeNull();
        updated2!.Scheduled.Should().Be(DateTime.SpecifyKind(newdate2, DateTimeKind.Utc));
        updated2.Status.Should().Be(ScheduleStatus.Scheduled);
    }
}