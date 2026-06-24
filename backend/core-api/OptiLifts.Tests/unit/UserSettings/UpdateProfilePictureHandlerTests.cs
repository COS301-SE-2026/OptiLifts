using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Users;

namespace OptiLifts.Tests.Unit.UserSettings;

public class UpdateProfilePictureHandlerTests
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
    public async Task Handle_ValidUserAndImage_UploadsAndUpdatesProfileImageUrl()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            ProfileImageUrl = null
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        blobMock.Setup(b => b.UploadFileAsync(
            It.IsAny<Stream>(),
            "avatar.png",
            "image/png",
            "profile-pictures",
            It.IsAny<CancellationToken>()
        )).ReturnsAsync("https://image.url/profile-pictures/new-avatar.png");

        var handler = new UpdateProfilePictureHandler(context, blobMock.Object);

        using var memoryStream = new MemoryStream(new byte[] { 0, 1, 2, 3 });
        var command = new UploadProfilePictureCommand(
            userId,
            memoryStream,
            "avatar.png",
            "image/png"
        );

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be("https://image.url/profile-pictures/new-avatar.png");

        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser.Should().NotBeNull();
        updatedUser.ProfileImageUrl.Should().Be("https://image.url/profile-pictures/new-avatar.png");
    }
}