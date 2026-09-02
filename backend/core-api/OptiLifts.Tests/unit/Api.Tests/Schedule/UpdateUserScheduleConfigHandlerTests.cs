using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling.Reschedule;

namespace OptiLifts.Tests.Api.Tests.Schedule;

public sealed class UpdateUserScheduleConfigHandlerTests
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
    public async Task Handle_CreatesNewConfigandUpdatesFields_WHenConfidDoesNotExist()
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

        var newconfig = new UserScheduleConfigDto(
            DynamicSchedulerEnabled: true,
            MaxWorkoutsPerDay: 2,
            MinMuscleRestHours: 36,
            RestDays: new List<string> { "Wednesday", "Sunday" },
            CycleWindowLengthDays: 7,
            CycleStartDate: DateTime.UtcNow.Date
        );

        var handler = new UpdateUserScheduleConfigHandler(db);
        var command = new UpdateUserScheduleConfigCommand(userId, newconfig);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.MaxWorkoutsPerDay.Should().Be(2);
        result.MinMuscleRestHours.Should().Be(36);
        result.RestDays.Should().BeEquivalentTo(new[] { "Wednesday", "Sunday" });

        var createdinDb = await db.UserScheduleConfigs.FirstOrDefaultAsync(c => c.UserId == userId);
        createdinDb.Should().NotBeNull();
        createdinDb!.MaxWorkoutsPerDay.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UpdatesExistingConfig_WhenConfigAlreadyExists()
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
            MaxWorkoutsPerDay = 2,
            MinMuscleRestHours = 72,
            RestDays = new List<string> { "Saturday", "Sunday" },
        });
        await db.SaveChangesAsync();

        var newconfig = new UserScheduleConfigDto(
            DynamicSchedulerEnabled: false,
            MaxWorkoutsPerDay: 3,
            MinMuscleRestHours: 72,
            RestDays: new List<string> { "Saturday", "Sunday" },
            CycleWindowLengthDays: 14,
            CycleStartDate: DateTime.UtcNow.Date
        );

        var handler = new UpdateUserScheduleConfigHandler(db);
        var command = new UpdateUserScheduleConfigCommand(userId, newconfig);
        var result = await handler.Handle(command, CancellationToken.None);

        result.DynamicSchedulerEnabled.Should().BeFalse();
        result.MaxWorkoutsPerDay.Should().Be(3);
        result.MinMuscleRestHours.Should().Be(72);
        result.RestDays.Should().BeEquivalentTo(new[] { "Saturday", "Sunday" });
    }
}