using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.UpdateMissedSessions;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
namespace OptiLifts.Tests.Api.Tests;

public sealed class UpdateMissedSessionsHandlerTests
{
    public static async Task<OptiLiftsDbContext> MemoryDBCreation() //avoiding dup
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
    public async Task Handle_UpdatesIncompleteSessionsAsMissed()
    {
        using var db = await MemoryDBCreation();
        var userId = Guid.NewGuid();

        var user1 = new User
        {
            Id = userId,
            Email = "sessiontesting@example.com",
            PasswordHash = "xy",
            EmailHash = "teststuffhash",
            DisplayName = "MeTest"
        };
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Chest day",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var pastEntry = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.AddDays(-2),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(pastEntry);
        await db.SaveChangesAsync();

        var completedEntry = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.AddDays(-2),
            Status = ScheduleStatus.Completed
        };
        db.ScheduledEntries.Add(completedEntry);
        await db.SaveChangesAsync();

        var futureSession = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.AddDays(1),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(futureSession);
        await db.SaveChangesAsync();
        
        var handler = new UpdateMissedSessionsHandler(db);
        var result = await handler.Handle(new UpdateMissedSessionsCommand(userId), CancellationToken.None);
        result.UpdatedCount.Should().Be(1);

        var updatedPast = await db.ScheduledEntries.FindAsync(pastEntry.Id);
        updatedPast!.Status.Should().Be(ScheduleStatus.Missed);

        var updateCompleted = await db.ScheduledEntries.FindAsync(completedEntry.Id);
        updateCompleted!.Status.Should().Be(ScheduleStatus.Completed);
        var updatedFuture = await db.ScheduledEntries.FindAsync(futureSession.Id);
        updatedFuture!.Status.Should().Be(ScheduleStatus.Scheduled);
    }

    [Fact]
    public async Task Handle_DoesNotModifyUnOwned()
    {
        using var db = await MemoryDBCreation();
        var userId = Guid.NewGuid();
        var altUserId = Guid.NewGuid();

        var user1 = new User
        {
            Id = userId,
            Email = "sessiontesting@example.com",
            PasswordHash = "xy",
            EmailHash = "teststuffhash",
            DisplayName = "MeTest"
        };
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        var user2 = new User
        {
            Id = altUserId,
            Email = "idontliketesting@example.com",
            PasswordHash = "yx",
            EmailHash = "lotstesting",
            DisplayName = "YouTest"
        };
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "notmylegs",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var entry = new ScheduledEntry
        {
            UserId = altUserId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.AddDays(-2),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new UpdateMissedSessionsHandler(db);
        var result = await handler.Handle(new UpdateMissedSessionsCommand(userId), CancellationToken.None);
        result.UpdatedCount.Should().Be(0);

        var entryindb = await db.ScheduledEntries.FindAsync(entry.Id);
        entryindb!.Status.Should().Be(ScheduleStatus.Scheduled);
    }
}