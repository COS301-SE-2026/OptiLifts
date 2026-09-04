using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Users;

namespace OptiLifts.Tests.Unit.UserSettings;

public class UserSettingsHandlerTests
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
    public async Task Handle_UserExists_ReturnsMappedUserSettingsDto()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            DisplayName = "Jordan",
            Bio = "Stronk",
            Sex = "Male",
            DateOfBirth = "2005/11/21",
            Weight = "10.3",
            Height = "194.2",
            LightTheme = false,
            Metric = true,
            ProfileImageUrl = "https://fake.url/image.jpg"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UserSettingsHandler(context);
        var query = new GetUserSettingsQuery(userId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Profile.DisplayName.Should().Be("Jordan");
        result.Profile.Bio.Should().Be("Stronk");
        result.Profile.Sex.Should().Be("Male");

        var date = new DateTime(2005, 11, 21, 0, 0, 0, DateTimeKind.Utc);
        result.Profile.DateOfBirth.Should().BeSameDateAs(date);

        result.Profile.Weight.Should().BeApproximately(10.3, 0.01);
        result.Profile.Height.Should().BeApproximately(194.2, 0.01);

        result.Profile.ProfilePictureUrl.Should().Be("https://fake.url/image.jpg");
        result.Preferences.Theme.Should().Be("dark");
        result.Preferences.Units.Should().Be("metric");
        result.Security.HasPassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ThrowsKeyNotFoundException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var handler = new UserSettingsHandler(context);
        var query = new GetUserSettingsQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}