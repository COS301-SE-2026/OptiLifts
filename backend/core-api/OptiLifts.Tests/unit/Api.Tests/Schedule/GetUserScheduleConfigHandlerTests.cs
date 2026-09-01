using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling.Reschedule;

namespace OptiLifts.Tests.Api.Tests.Schedule;

public sealed class GetUserScheduleConfigHandlerTests
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
    public async Task Handle_ReturnsExistingConfig_WhenConfigExists()
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

        db.UserScheduleConfigs.Add(new UserScheduleConfig
        {
            UserId = userId,
            DynamicSchedulerEnabled = true,
            MaxWorkoutsPerDay = 2,
            MinMuscleRestHours  = 72,
            RestDays = new List<string> {"Saturday","Sunday"},
            CycleWindowLengthDays = 14,
            CycleStartDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var handler = new GetUserScheduleConfigHandler(db);
        var command = new GetUserScheduleConfigQuery(userId);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        result.DynamicSchedulerEnabled.Should().BeTrue();
        result.MaxWorkoutsPerDay.Should().Be(3);
        result.MinMuscleRestHours.Should().Be(72);
        result.RestDays.Should().Contain(new[] { "Saturday", "Sunday" });
        result.CycleWindowLengthDays.Should().Be(14);
    }

    [Fact]
    public async Task Handle_CreatedandReturnsDefaultConfig_WhenNoConfigExists()
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
        await db.SaveChangesAsync();

        var handler = new GetUserScheduleConfigHandler(db);
        var command = new GetUserScheduleConfigQuery(userId);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        result.DynamicSchedulerEnabled.Should().BeTrue();
        result.MaxWorkoutsPerDay.Should().Be(1);
        result.MinMuscleRestHours.Should().Be(48);
        result.RestDays.Should().ContainSingle().Which.Should().Be("Sunday");
        
        var createdinDb = await db.UserScheduleConfigs.FirstOrDefaultAsync(c => c.UserId == userId);
        createdinDb.Should().NotBeNull();
    }
}