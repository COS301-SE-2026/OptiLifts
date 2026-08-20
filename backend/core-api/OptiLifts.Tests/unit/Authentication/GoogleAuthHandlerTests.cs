using FluentAssertions;
using Google.Apis.Auth;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Google;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Tests.Unit.Authentication;

public class GoogleAuthHandlerTests
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
    public async Task Handle_NewUser_CreatesUserWithGoogleIdAndReturnsTokens()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var googleAuthMock = new Mock<IGoogleAuthService>();
        googleAuthMock
            .Setup(g => g.ValidateIdTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfoDto("google-sub-123", "newuser@example.com", "New User", "https://photo.url"));

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.CreateToken(It.IsAny<User>())).Returns("MOCK_JWT_TOKEN");

        var handler = new GoogleAuthHandler(context, googleAuthMock.Object, jwtMock.Object);
        var result = await handler.Handle(new GoogleAuthCommand("valid-token"), CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("MOCK_JWT_TOKEN");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("newuser@example.com");
        result.User.DisplayName.Should().Be("New User");

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == "google-sub-123");
        savedUser.Should().NotBeNull();
        savedUser!.GoogleId.Should().Be("google-sub-123");
        savedUser.PasswordHash.Should().BeNull();
        savedUser.ProfileImageUrl.Should().Be("https://photo.url");
    }

    [Fact]
    public async Task Handle_ExistingUserWithMatchingEmail_LinksGoogleId()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            EmailHash = EmailHasher.HashEmail("existing@example.com"),
            DisplayName = "Existing User",
            PasswordHash = "HASHED_PASSWORD",
            GoogleId = null
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var googleAuthMock = new Mock<IGoogleAuthService>();
        googleAuthMock
            .Setup(g => g.ValidateIdTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfoDto("google-sub-999", "existing@example.com", "Existing User", null));

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.CreateToken(It.IsAny<User>())).Returns("MOCK_JWT_TOKEN");

        var handler = new GoogleAuthHandler(context, googleAuthMock.Object, jwtMock.Object);
        var result = await handler.Handle(new GoogleAuthCommand("valid-token"), CancellationToken.None);

        result.Should().NotBeNull();
        result.User.Email.Should().Be("existing@example.com");

        var updatedUser = await context.Users.FirstAsync(u => u.Id == existingUser.Id);
        updatedUser.GoogleId.Should().Be("google-sub-999");
        updatedUser.PasswordHash.Should().Be("HASHED_PASSWORD");
    }

    [Fact]
    public async Task Handle_ExistingUserByGoogleId_AuthenticatesSuccessfully()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "googleuser@example.com",
            EmailHash = EmailHasher.HashEmail("googleuser@example.com"),
            DisplayName = "Google User",
            GoogleId = "google-sub-777",
            PasswordHash = null
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var googleAuthMock = new Mock<IGoogleAuthService>();
        googleAuthMock
            .Setup(g => g.ValidateIdTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfoDto("google-sub-777", "googleuser@example.com", "Google User", null));

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.CreateToken(It.IsAny<User>())).Returns("MOCK_JWT_TOKEN");

        var handler = new GoogleAuthHandler(context, googleAuthMock.Object, jwtMock.Object);
        var result = await handler.Handle(new GoogleAuthCommand("valid-token"), CancellationToken.None);

        result.Should().NotBeNull();
        result.User.Id.Should().Be(existingUser.Id);
        result.AccessToken.Should().Be("MOCK_JWT_TOKEN");
    }

    [Fact]
    public async Task Handle_EmptyToken_ThrowsArgumentException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var googleAuthMock = new Mock<IGoogleAuthService>();
        var jwtMock = new Mock<IJwtTokenService>();

        var handler = new GoogleAuthHandler(context, googleAuthMock.Object, jwtMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new GoogleAuthCommand(""), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsInvalidJwtException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var googleAuthMock = new Mock<IGoogleAuthService>();
        googleAuthMock
            .Setup(g => g.ValidateIdTokenAsync("bad-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidJwtException("Invalid token signature"));

        var jwtMock = new Mock<IJwtTokenService>();

        var handler = new GoogleAuthHandler(context, googleAuthMock.Object, jwtMock.Object);

        await Assert.ThrowsAsync<InvalidJwtException>(() => handler.Handle(new GoogleAuthCommand("bad-token"), CancellationToken.None));
    }
}
