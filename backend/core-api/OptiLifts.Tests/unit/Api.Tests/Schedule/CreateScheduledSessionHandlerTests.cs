using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
namespace OptiLifts.Tests.Api.Tests;

public sealed class CreateScheduledSessionHandlerTests
{
    public static async Task<OptiLiftsDbContext> MemoryDBCreation() //avoiding dup
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;
        var context = new OptiLiftsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task Handle_ReturnNull_WorkoutNotExist_OrNotOwned()
    {
        using var db = await MemoryDBCreation();
        var userId = Guid.NewGuid();
        var altUserId = Guid.NewGuid();

        var user1 = new User
        {
            Id = userId,
            Email = "user1@example.com",
            PasswordHash = "xy",
            EmailHash = "user1hash",
            DisplayName = "User One"
        };
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        var user2 = new User
        {
            Id = altUserId,
            Email = "user2@example.com",
            PasswordHash = "yx",
            EmailHash = "user2hash",
            DisplayName = "User Two"
        };
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var notownedWorkout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Push Day",
            CreatedBy = altUserId //ie not owned 
        };
        db.Workouts.Add(notownedWorkout);
        await db.SaveChangesAsync();


        var handler = new CreateScheduledSessionHandler(db);
        var command = new CreateScheduledSessionCommand(
            UserId: userId,
            WorkoutId: notownedWorkout.Id,
            ScheduledAt: DateTime.UtcNow,
            Status: ScheduleStatus.Scheduled
        );

        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ScheduleSession_NoRepeat()
    {
        using var db = await MemoryDBCreation();
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "testing@example.com",
            PasswordHash = "xy",
            DisplayName = "User"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Legs",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var scheduledDate = new DateTime(2026, 12,1,9,30,0, DateTimeKind.Utc);
        var handler = new CreateScheduledSessionHandler(db);
        var command = new CreateScheduledSessionCommand(
            UserId: userId,
            WorkoutId: workout.Id,
            ScheduledAt: scheduledDate,
            Status: ScheduleStatus.Completed
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.WorkoutId.Should().Be(workout.Id);
        result.ScheduledAt.Should().Be(scheduledDate);
        result.Status.Should().Be(ScheduleStatus.Completed);
        var saved = await db.ScheduledEntries.SingleAsync(e => e.Id == result.Id);
        saved.Status.Should().Be(ScheduleStatus.Completed);
    }

    [Fact]
    public async Task Handle_ScheduleMultiple_RepeatWeeekly()
    {
        using var db = await MemoryDBCreation();
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "moretests@example.com",
            PasswordHash = "xy",
            DisplayName = "Tester"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Pushing",
            CreatedBy = userId 
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var start = new DateTime(2026,7,5,10,0,0, DateTimeKind.Utc);
        var end = new DateTime(2026,7,19,10,0,0, DateTimeKind.Utc);

        var handler = new CreateScheduledSessionHandler(db);
        var command = new CreateScheduledSessionCommand(
            UserId: userId,
            WorkoutId: workout.Id,
            ScheduledAt: start,
            Status: ScheduleStatus.Scheduled,
            Repeat: "week",
            Interval: 1,
            Until: end
        );
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var entries = await db.ScheduledEntries
            .Where(e => e.UserId == userId && e.WorkoutId == workout.Id)
            .OrderBy(e => e.Scheduled)
            .ToListAsync();
        entries.Should().HaveCount(3);
        entries[0].Scheduled.Should().Be(start);
        entries[1].Scheduled.Should().Be(start.AddDays(7));
        entries[2].Scheduled.Should().Be(start.AddDays(14));
    }

}
        