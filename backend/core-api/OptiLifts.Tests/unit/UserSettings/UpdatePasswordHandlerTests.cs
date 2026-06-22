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

public class UpdatePasswordHandlerTests
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
    public async Task Handle_ValidCredentialsAndComplexity_UpdatesPasswordHash()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            PasswordHash = "Password123!"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Verify("Password123!", "Password123!")).Returns(true);
        hasherMock.Setup(h => h.Hash("NewPassw123!")).Returns("NEW_HASH");

        var handler = new ChangePasswordHandler(context, hasherMock.Object);
        var command = new UpdatePasswordCommand(userId, "Password123!", "NewPassw123!");

        await handler.Handle(command, CancellationToken.None);
        var updatedUser = await context.Users.FindAsync(userId);
        
        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordHash.Should().Be("NEW_HASH");
    }

    [Fact]
    public async Task Handle_IncorrectCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            PasswordHash = "Password123!"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Verify("Password123!", "IncorrectP123!")).Returns(false);

        var handler = new ChangePasswordHandler(context, hasherMock.Object);
        var command = new UpdatePasswordCommand(userId, "IncorrectP123!", "NewPassw123!");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PasswordTooShort_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "jordan@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var handler = new ChangePasswordHandler(context, hasherMock.Object);
        var command = new UpdatePasswordCommand(userId, "Password123!", "7charac"); 

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingSpecialCharacter_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "jordan@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var handler = new ChangePasswordHandler(context, hasherMock.Object);
        var command = new UpdatePasswordCommand(userId, "Password123!", "NoSpecial123"); 

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoDigits_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "jordan@gmail.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var handler = new ChangePasswordHandler(context, hasherMock.Object);
        var command = new UpdatePasswordCommand(userId, "Password123!", "NoDigits!"); 

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
}