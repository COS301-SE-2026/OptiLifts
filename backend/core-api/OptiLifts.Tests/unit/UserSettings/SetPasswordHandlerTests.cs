using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Users;

namespace OptiLifts.Tests.Unit.UserSettings;

public class SetPasswordHandlerTests
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
    public async Task Handle_OAuthUserWithoutExistingPassword_SetsPasswordSuccessfully()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "googleuser@gmail.com",
            GoogleId = "google-sub-123",
            PasswordHash = null
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Hash("NewPassw123!")).Returns("NEW_HASH");

        var handler = new SetPasswordHandler(context, hasherMock.Object);
        var command = new SetPasswordCommand(userId, "NewPassw123!");

        await handler.Handle(command, CancellationToken.None);
        var updatedUser = await context.Users.FindAsync(userId);

        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordHash.Should().Be("NEW_HASH");
    }

    [Fact]
    public async Task Handle_UserAlreadyHasPassword_ThrowsInvalidOperationException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "existinguser@gmail.com",
            PasswordHash = "EXISTING_HASH"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var handler = new SetPasswordHandler(context, hasherMock.Object);
        var command = new SetPasswordCommand(userId, "NewPassw123!");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var hasherMock = new Mock<IPasswordHasher>();
        var handler = new SetPasswordHandler(context, hasherMock.Object);
        var command = new SetPasswordCommand(userId, "NewPassw123!");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PasswordTooShort_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "googleuser@gmail.com", PasswordHash = null };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var handler = new SetPasswordHandler(context, hasherMock.Object);
        var command = new SetPasswordCommand(userId, "7charac");

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
}
