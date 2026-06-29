using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
using OptiLifts.Domain.Users;
namespace OptiLifts.Tests.Api.Tests;

public sealed class GetScheduleHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmpty_NoScheduleEntries()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var handler = new GetScheduleHandler(db);

        var result = await handler.Handle(
            new GetScheduleQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(-1)), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnEntries_WithWorkouts()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "x",
            DisplayName = "Test person"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Hypertrophy",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var schedule = new DateTime(2026,6,27,10,0,0, DateTimeKind.Utc);
        var entry = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = schedule,
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new GetScheduleHandler(db);
        var result = await handler.Handle(
            new GetScheduleQuery(userId, new DateTime(2026,6,25,0,0,0, DateTimeKind.Utc), new DateTime(2026,6,28,0,0,0, DateTimeKind.Utc)), CancellationToken.None);

        result.Should().HaveCount(1);
        var first = result[0];
        first.Id.Should().Be(entry.Id);
        first.WorkoutId.Should().Be(workout.Id);
        first.WorkoutName.Should().Be("Hypertrophy");
        first.Scheduled.Should().Be(schedule);
        first.Status.Should().Be("Scheduled");
    }
}