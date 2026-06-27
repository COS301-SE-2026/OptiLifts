using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Users;

namespace OptiLifts.Tests.Unit.UserSettings;

public class UpdateUserPreferencesHandlerTests
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
    public async Task Handle_UserExists_UpdatesToLightAndMetric()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            LightTheme = true,
            Metric = false
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserPreferencesHandler(context);
        var command = new UpdateUserPreferencesCommand(userId, "dark", "metric");

        await handler.Handle(command, CancellationToken.None);
        var updatedUser = await context.Users.FindAsync(userId);

        updatedUser.Should().NotBeNull();
        updatedUser.LightTheme.Should().BeFalse();
        updatedUser.Metric.Should().BeTrue();
    }
}