using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.UpdateScheduledSessionStatus;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
namespace OptiLifts.Tests.Api.Tests;

public sealed class UpdateScheduledSessionStatusHandlerTests
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
    public async Task Handle_ReturnNull_WhenSessionNonexistent()
    {
        using var db = await MemoryDBCreation();
        var handler = new UpdateScheduledSessionStatusHandler(db);
        var command = new UpdateScheduledSessionStatusCommand(Guid.NewGuid(), Guid.NewGuid(), ScheduleStatus.Completed);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeNull();
    }
    [Fact]
    public async Task Handle_ReturnNull_WhenSessionNotOwned()
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
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.AddDays(1),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new UpdateScheduledSessionStatusHandler(db);
        var command = new UpdateScheduledSessionStatusCommand(altUserId, entry.Id, ScheduleStatus.Completed);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeNull();

        var entrydb = await db.ScheduledEntries.SingleAsync(e => e.Id == entry.Id);
        entrydb.Status.Should().Be(ScheduleStatus.Scheduled); //check4 unchanged status
    }
    [Fact]
    public async Task Handle_UpdateStatus_SessionExistsAndOwned()
    {
        using var db = await MemoryDBCreation();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "testingmystuff@example.com",
            PasswordHash = "y",
            DisplayName = "Test mense"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Arms+Upper",
            CreatedBy = userId 
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var entry = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.AddDays(2),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new UpdateScheduledSessionStatusHandler(db);
        var command = new UpdateScheduledSessionStatusCommand(
            userId,
            entry.Id, ScheduleStatus.Missed);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        result.Id.Should().Be(entry.Id);
        result.Status.Should().Be(ScheduleStatus.Missed);
        var entrydb = await db.ScheduledEntries.SingleAsync(e => e.Id == entry.Id);
        entrydb.Status.Should().Be(ScheduleStatus.Missed); //check4 changes status
    }
}