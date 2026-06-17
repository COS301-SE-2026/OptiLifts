using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Refresh;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Tests.Authentication;

public class RefreshTokenHandlerTests
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
    public async Task Handle_ValidToken_ReturnsNewTokensAndUpdatesDatabase()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var cont = CreateContext(connection);

        var token = "oldRefreshToken";
        var hashedToken = TokenHelper.HashToken(token);

        var user = new User
        {
            Email = "jordan@gmail.com",
            EmailHash = "emailHash",
            PasswordHash = "passwordHash",
            DisplayName = "Jordan",
            RefreshTokenHash = hashedToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        cont.Users.Add(user);
        await cont.SaveChangesAsync();

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.CreateToken(It.IsAny<User>())).Returns("newToken");
        var refHandler = new RefreshTokenHandler(cont, jwtMock.Object);
        var refCommand = new RefreshTokenCommand(token);

        var result = await refHandler.Handle(refCommand, CancellationToken.None);

        //check dto updated
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("newToken");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace().And.NotBe(token);
        result.User.Email.Should().Be("jordan@gmail.com");

        //check db for hash and expiration date
        var userInDb = await cont.Users.SingleAsync(u => u.Id == user.Id);
        userInDb.RefreshTokenHash.Should().Be(TokenHelper.HashToken(result.RefreshToken));
        userInDb.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorizedAccessException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var cont = CreateContext(connection);

        var jwtMock = new Mock<IJwtTokenService>();
        var refHandler = new RefreshTokenHandler(cont, jwtMock.Object);
        var refCommand = new RefreshTokenCommand("TokenDoesntExist");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => refHandler.Handle(refCommand, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TokenExpired_ThrowsUnauthorizedAccessException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var cont = CreateContext(connection);

        var token = "expiredToken";
        var hashedToken = TokenHelper.HashToken(token);

        var user = new User
        {
            Email = "jordan@gmail.com",
            EmailHash = "emailHash",
            PasswordHash = "passwordHash",
            DisplayName = "Jordan",
            RefreshTokenHash = hashedToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(-1)
        };

        cont.Users.Add(user);
        await cont.SaveChangesAsync();

        var jwtMock = new Mock<IJwtTokenService>();
        var handler = new RefreshTokenHandler(cont, jwtMock.Object);
        var command = new RefreshTokenCommand(token);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }
}