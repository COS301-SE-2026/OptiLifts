using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
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

   
}
