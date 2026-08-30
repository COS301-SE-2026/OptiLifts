using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Scheduling.DeleteScheduledSession;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
namespace OptiLifts.Tests.Api.Tests;

public sealed class DeleteScheduledSessionHandlerTests
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
    public async Task Handle_ReturnFalse_WhenNoSession()
    {
        using var db = await MemoryDBCreation();
        var handler = new DeleteScheduledSessionHandler(db, new Mock<IGoogleCalendarService>().Object);
        var command = new DeleteScheduledSessionCommand(
            Guid.NewGuid(),
            Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnFalse_WhenNotOwnedSession()
    {
        using var db = await MemoryDBCreation();
        var userId = Guid.NewGuid();
        var altUserId = Guid.NewGuid();

        var user1 = new User
        {
            Id = userId,
            Email = "userONE@example.com",
            PasswordHash = "xy",
            EmailHash = "userONEhash",
            DisplayName = "User ONE"
        };
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        var user2 = new User
        {
            Id = altUserId,
            Email = "userTWO@example.com",
            PasswordHash = "yx",
            EmailHash = "userTWOhash",
            DisplayName = "User TWO"
        };
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        var notOwnedWorkout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "LEGS",
            CreatedBy = altUserId //ie not owned 
        };
        db.Workouts.Add(notOwnedWorkout);
        await db.SaveChangesAsync();

        var entry = new ScheduledEntry
        {
            UserId = altUserId,
            WorkoutId = notOwnedWorkout.Id,
            Scheduled = DateTime.UtcNow.AddDays(1),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new DeleteScheduledSessionHandler(db, new Mock<IGoogleCalendarService>().Object);
        var command = new DeleteScheduledSessionCommand(
            userId,
            altUserId);

        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeFalse();
        var notdeleted = await db.ScheduledEntries.AnyAsync(e => e.Id == entry.Id);
        notdeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Deletes_WhenExistAndOwned()
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

        var handler = new DeleteScheduledSessionHandler(db, new Mock<IGoogleCalendarService>().Object);
        var command = new DeleteScheduledSessionCommand(
            userId,
            entry.Id);

        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeTrue();
        var notdeleted = await db.ScheduledEntries.AnyAsync(e => e.Id == entry.Id);
        notdeleted.Should().BeFalse();

    }

}
