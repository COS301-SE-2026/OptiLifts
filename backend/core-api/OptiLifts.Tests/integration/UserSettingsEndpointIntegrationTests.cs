using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Users;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class UserSettingsEndpointIntegrationTests : IntegrationTestBase
{
    public UserSettingsEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private async Task<Guid> SeedAuthenticatedUserAsync(string email)
    {
        var userId = await SeedUserAsync(email);
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
        return userId;
    }

    [Fact]
    public async Task GetUserSettings_Succeeds()
    {
        await SeedAuthenticatedUserAsync("jordan@gmail.com");
        var response = await Client.GetAsync("/api/users/me/settings");

        response.EnsureSuccessStatusCode();
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();

        settings.Should().NotBeNull();
        settings.Profile.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetUserSettings_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        var response = await Client.GetAsync("/api/users/me/settings");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfileDetails_Succeeds()
    {
        await SeedAuthenticatedUserAsync("jordan@gmail.com");

        var request = JsonContent.Create(new
        {
            DisplayName = "Jordan",
            Bio = "New bio",
            Sex = "Male",
            DateOfBirth = "2005-11-22T00:00:00Z",
            Weight = 1, //skinny legend
            Height = 194.0
        });

        var response = await Client.PatchAsync("/api/users/me/profileDetails", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync("/api/users/me/settings");
        var settings = await getResponse.Content.ReadFromJsonAsync<UserSettingsDto>();

        settings.Should().NotBeNull();
        settings.Profile.DisplayName.Should().Be("Jordan");
        settings.Profile.Bio.Should().Be("New bio");
        settings.Profile.Sex.Should().Be("Male");
        settings.Profile.DateOfBirth.Should().Be(new DateTime(2005, 11, 22, 0, 0, 0, DateTimeKind.Utc));
        settings.Profile.Weight.Should().Be(1);
        settings.Profile.Height.Should().Be(194.0);
    }

    [Fact]
    public async Task UpdatePreferences_Succeeds()
    {
        await SeedAuthenticatedUserAsync("jordan@gmail.com");
        var request = JsonContent.Create(new
        {
            Theme = "dark",
            Units = "imperial"
        });

        var response = await Client.PatchAsync("/api/users/me/preferences", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync("/api/users/me/settings");
        var settings = await getResponse.Content.ReadFromJsonAsync<UserSettingsDto>();

        settings!.Preferences.Theme.Should().Be("dark");
        settings.Preferences.Units.Should().Be("imperial");
    }

    [Fact]
    public async Task UpdatePreferences_MissingFields_ReturnsBadRequest()
    {
        await SeedAuthenticatedUserAsync("pref-bad@optilifts.com");
        var request = JsonContent.Create(new
        {
            Theme = "",
            Units = "imperial"
        });
        var response = await Client.PatchAsync("/api/users/me/preferences", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProfilePicture_Succeeds()
    {
        await SeedAuthenticatedUserAsync("jordan@gmail.com");
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "profilePicture", "test.png");

        var response = await Client.PatchAsync("/api/users/me/profilePicture", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("profilePictureUrl");

        var getResponse = await Client.GetAsync("/api/users/me/settings");
        var settings = await getResponse.Content.ReadFromJsonAsync<UserSettingsDto>();
        settings!.Profile.ProfilePictureUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UploadProfilePicture_UnsupportedImageType_ReturnsBadRequest()
    {
        await SeedAuthenticatedUserAsync("pic-bad-bmp@optilifts.com");
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("hola"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/svg");
        content.Add(fileContent, "profilePicture", "test.svg");
        var response = await Client.PatchAsync("/api/users/me/profilePicture", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var responseString = await response.Content.ReadAsStringAsync();

        responseString.Should().Contain("File must be an image(JPEG, PNG or WebP)");
    }

    [Fact]
    public async Task UploadProfilePicture_NotAnImage_ReturnsBadRequest()
    {
        await SeedAuthenticatedUserAsync("pic-bad-txt@optilifts.com");
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("I am totally an image, trust"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "profilePicture", "test.txt");
        var response = await Client.PatchAsync("/api/users/me/profilePicture", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteProfilePicture_Succeeds()
    {
        await SeedAuthenticatedUserAsync("del-pic-good@optilifts.com");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("delete this one"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "profilePicture", "test.png");
        await Client.PatchAsync("/api/users/me/profilePicture", content);

        var response = await Client.DeleteAsync("/api/users/me/deleteProfilePicture");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync("/api/users/me/settings");
        var settings = await getResponse.Content.ReadFromJsonAsync<UserSettingsDto>();
        settings!.Profile.ProfilePictureUrl.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfilePicture_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        var response = await Client.DeleteAsync("/api/users/me/deleteProfilePicture");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePassword_Succeeds()
    {
        await SeedAuthenticatedUserAsync("pass-good@optilifts.com");
        var request = new
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/api/users/me/updatePassword", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdatePassword_IncorrectCurrentPassword_ReturnsBadRequest()
    {
        await SeedAuthenticatedUserAsync("jordan@gmail.com");
        var request = new
        {
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/api/users/me/updatePassword", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePassword_WeakNewPassword_ReturnsBadRequest()
    {
        await SeedAuthenticatedUserAsync("jordan@gmail.com");
        var request = new
        {
            CurrentPassword = "Password123!",
            NewPassword = "lol"
        };

        var response = await Client.PostAsJsonAsync("/api/users/me/updatePassword", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePassword_MissingCurrentPassword_ReturnsBadRequest()
    {
        await SeedAuthenticatedUserAsync("jordan@gmail.com");
        var request = new
        {
            CurrentPassword = "",
            NewPassword = "NewPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/api/users/me/updatePassword", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPassword_OAuthUserWithoutCurrentPassword_Succeeds()
    {
        var email = "oauth-setpass@optilifts.com";
        var userId = Guid.NewGuid();

        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLifts.Infrastructure.Database.OptiLiftsDbContext>();
            db.Users.Add(new OptiLifts.Domain.Users.User
            {
                Id = userId,
                Email = email,
                EmailHash = OptiLifts.Infrastructure.Security.EmailHasher.HashEmail(email),
                GoogleId = "google-sub-setpass-1",
                PasswordHash = null,
                DisplayName = "OAuth User"
            });
            db.Folders.Add(new OptiLifts.Domain.Workouts.Folder { Name = "Default", UserId = userId });
            await db.SaveChangesAsync();
        }

        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        // Verify settings returns HasPassword = false
        var settingsResponse = await Client.GetAsync("/api/users/me/settings");
        settingsResponse.EnsureSuccessStatusCode();
        var settings = await settingsResponse.Content.ReadFromJsonAsync<UserSettingsDto>();
        settings.Should().NotBeNull();
        settings!.Security.HasPassword.Should().BeFalse();

        // Set password for OAuth user
        var request = new
        {
            NewPassword = "BrandNewPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/api/users/me/setPassword", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        // Verify settings now returns HasPassword = true
        var updatedSettingsResponse = await Client.GetAsync("/api/users/me/settings");
        var updatedSettings = await updatedSettingsResponse.Content.ReadFromJsonAsync<UserSettingsDto>();
        updatedSettings!.Security.HasPassword.Should().BeTrue();
    }
}
