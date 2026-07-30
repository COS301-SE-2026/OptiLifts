using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Users;

namespace OptiLifts.Tests.Unit.UserSettings;

public class UpdateProfileDetailsHandlerTests
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
    public async Task Handle_UserExists_UpdatesProfileDetails()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            DisplayName = "Jdawg"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateProfileDetailsHandler(context);
        var command = new UpdateProfileDetailsCommand(userId, "JerDawg", "Bang", "Other", "2005-11-21", 100, 194.2);

        await handler.Handle(command, CancellationToken.None);

        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser.Should().NotBeNull();
        updatedUser!.DisplayName.Should().Be("JerDawg");
        updatedUser.Bio.Should().Be("Bang");
        updatedUser.Sex.Should().Be("Other");
        updatedUser.DateOfBirth.Should().Be("2005-11-21");
        updatedUser.Weight.Should().Be("100");
        updatedUser.Height.Should().Be("194.2");
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ThrowsKeyNotFoundException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var handler = new UpdateProfileDetailsHandler(context);
        var command = new UpdateProfileDetailsCommand(Guid.NewGuid(), "Jordan", null, null, null, null, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }
}