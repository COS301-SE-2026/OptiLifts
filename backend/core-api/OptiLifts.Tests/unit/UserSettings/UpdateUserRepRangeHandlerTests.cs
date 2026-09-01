using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Users;

namespace OptiLifts.Tests.Unit.UserSettings;

public class UpdateUserRepRangeHandlerTests
{
    private static OptiLiftsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new OptiLiftsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Handle_RepRangeExists_UpdatesRangeAndType()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "update-range@optilifts.com",
            EmailHash = "hash-1",
            PasswordHash = "password-hash",
            DisplayName = "Range User"
        });

        var repRange = new UserRepRange
        {
            UserId = userId,
            ExerciseType = UserRepRangeExerciseType.Compound,
            LowerLimit = 8,
            UpperLimit = 10
        };

        context.UserRepRanges.Add(repRange);
        await context.SaveChangesAsync();

        var handler = new UpdateUserRepRangeHandler(context);
        var command = new UpdateUserRepRangeCommand(
            userId,
            repRange.Id,
            UserRepRangeExerciseType.Isolation,
            10,
            14
        );

        await handler.Handle(command, CancellationToken.None);

        var updated = await context.UserRepRanges.FindAsync(repRange.Id);
        updated.Should().NotBeNull();
        updated.ExerciseType.Should().Be(UserRepRangeExerciseType.Isolation);
        updated.LowerLimit.Should().Be(10);
        updated.UpperLimit.Should().Be(14);
    }

    [Fact]
    public async Task Handle_RepRangeDoesNotExist_ThrowsKeyNotFoundException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "missing-range@optilifts.com",
            EmailHash = "hash-2",
            PasswordHash = "password-hash",
            DisplayName = "Missing Range User"
        });
        await context.SaveChangesAsync();

        var handler = new UpdateUserRepRangeHandler(context);
        var command = new UpdateUserRepRangeCommand(
            userId,
            Guid.NewGuid(),
            UserRepRangeExerciseType.Compound,
            8,
            10
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateExerciseTypeForUser_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "duplicate-type@optilifts.com",
            EmailHash = "hash-3",
            PasswordHash = "password-hash",
            DisplayName = "Duplicate Type User"
        });

        var compoundRange = new UserRepRange
        {
            UserId = userId,
            ExerciseType = UserRepRangeExerciseType.Compound,
            LowerLimit = 8,
            UpperLimit = 10
        };

        var isolationRange = new UserRepRange
        {
            UserId = userId,
            ExerciseType = UserRepRangeExerciseType.Isolation,
            LowerLimit = 8,
            UpperLimit = 12
        };

        context.UserRepRanges.AddRange(compoundRange, isolationRange);
        await context.SaveChangesAsync();

        var handler = new UpdateUserRepRangeHandler(context);
        var command = new UpdateUserRepRangeCommand(
            userId,
            compoundRange.Id,
            UserRepRangeExerciseType.Isolation,
            9,
            11
        );

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
}
