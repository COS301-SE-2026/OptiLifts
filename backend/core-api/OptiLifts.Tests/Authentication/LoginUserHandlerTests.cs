using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Login;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Tests.Authentication;

public class LoginUserHandlerTests
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

    //normal login
    [Fact]
    public async Task HandleShouldReturnTokenAndUserWhenCredentialsAreValid()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        context.Users.Add(new User
        {
            Email = "jordan@gmail.com",
            PasswordHash = "HASHED_Passw0rd!",
            DisplayName = "Jordan",
            CreatedAt = new DateTime(2026, 01, 01, 12, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Verify("HASHED_Passw0rd!", "Passw0rd!")).Returns(true);

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.CreateToken(It.IsAny<User>())).Returns("FAKE_TOKEN");

        var handler = new LoginUserHandler(context, hasherMock.Object, jwtMock.Object);
        var lcmd = new LoginUserCommand("jordan@gmail.com", "Passw0rd!");

        var result = await handler.Handle(lcmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Token.Should().Be("FAKE_TOKEN");
        result.User.Email.Should().Be("jordan@gmail.com");
        result.User.DisplayName.Should().Be("Jordan");
        result.User.CreatedAt.Should().Be(new DateTime(2026, 01, 01, 12, 0, 0, DateTimeKind.Utc));

        hasherMock.Verify(h => h.Verify("HASHED_Passw0rd!", "Passw0rd!"), Times.Once);
        jwtMock.Verify(j => j.CreateToken(It.Is<User>(u => u.Email == "jordan@gmail.com")), Times.Once);
    }

    [Fact]
    public async Task HandleShouldThrowInvalidCredentialsExceptionWhenUserDoesNotExist()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var hasherMock = new Mock<IPasswordHasher>();
        var jwtMock = new Mock<IJwtTokenService>();

        var handler = new LoginUserHandler(context, hasherMock.Object, jwtMock.Object);
        var lcmd = new LoginUserCommand("jordan@gmail.com", "Passw0rd!");

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(lcmd, CancellationToken.None));

        hasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        jwtMock.Verify(j => j.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandleShouldThrowInvalidCredentialsExceptionWhenPasswordIsInvalid()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        context.Users.Add(new User
        {
            Email = "jordan@gmail.com",
            PasswordHash = "HASHED_Passw0rd!",
            DisplayName = "Jordan"
        });
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Verify("HASHED_Passw0rd!", "WrongPassword")).Returns(false);

        var jwtMock = new Mock<IJwtTokenService>();

        var handler = new LoginUserHandler(context, hasherMock.Object, jwtMock.Object);
        var lcmd = new LoginUserCommand("jordan@gmail.com", "WrongPassword");

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(lcmd, CancellationToken.None));

        hasherMock.Verify(h => h.Verify("HASHED_Passw0rd!", "WrongPassword"), Times.Once);
        jwtMock.Verify(j => j.CreateToken(It.IsAny<User>()), Times.Never);
    }
}