using System.Net.Http.Json;
using FluentAssertions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class AuthEndpointIntegrationTests : IntegrationTestBase
{
    public AuthEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }
    private record RegisterRequest(string DisplayName, string Email, string Password);
    private record LoginRequest(string Email, string Password);
    private record AuthUserDto(Guid Id, string DisplayName, string Email, DateTime CreatedAt);

    private async Task<User> SeedAuthUserAsync(string email, string displayName, string password)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            EmailHash = EmailHasher.HashEmail(email),
            DisplayName = displayName,
            PasswordHash = hasher.Hash(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static string ExtractCookiePair(IEnumerable<string> setCookieHeaders, string cookieName)
    {
        var cookie = setCookieHeaders.First(header => header.StartsWith($"{cookieName}=", StringComparison.OrdinalIgnoreCase));
        return cookie.Split(';', 2)[0];
    }

    [Fact]
    public async Task Register_Succeeds_ReturnsCookiesAndUser()
    {
        var request = new RegisterRequest("Jordan", "jordanRegister@gmail.com", "P@ssw0rd!");
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        response.EnsureSuccessStatusCode();

        response.Headers.Contains("Set-Cookie").Should().BeTrue();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("access_token="));
        cookies.Should().Contain(c => c.Contains("refresh_token="));

        var userDto = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        userDto.Should().NotBeNull();
        userDto.Email.Should().Be(request.Email);
        userDto.DisplayName.Should().Be(request.DisplayName);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = "jordanDuplicate@gmail.com";

        var first = new RegisterRequest("First", email, "Password123!");
        var second = new RegisterRequest("Second", email, "Password2234!");

        var r1 = await Client.PostAsJsonAsync("/api/auth/register", first);
        r1.EnsureSuccessStatusCode();

        var r2 = await Client.PostAsJsonAsync("/api/auth/register", second);
        r2.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_Succeeds_ReturnsCookiesAndUser()
    {
        var email = "jordanLogin@optilifts.com";
        var password = "Password123!";
        await SeedAuthUserAsync(email, "Jordan", password);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        response.EnsureSuccessStatusCode();

        response.Headers.Contains("Set-Cookie").Should().BeTrue();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("access_token="));
        cookies.Should().Contain(c => c.Contains("refresh_token="));

        var userDto = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        userDto.Should().NotBeNull();
        userDto.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var email = "jordanBadLogin@gmail.com";
        var password = "Password123!";
        await SeedAuthUserAsync(email, "Jordan", password);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword"));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_MissingFields_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new { DisplayName = "", Email = "", Password = "" });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidRefreshToken_ReturnsNewTokens()
    {
        var email = "jordan@gmail.com";
        var password = "Password123!";
        await SeedAuthUserAsync(email, "Jordan", password);

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var refreshCookie = ExtractCookiePair(cookies, "refresh_token");

        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshReq.Headers.Add("Cookie", refreshCookie);

        var refreshResponse = await Client.SendAsync(refreshReq);
        refreshResponse.EnsureSuccessStatusCode();
        var newCookies = refreshResponse.Headers.GetValues("Set-Cookie").ToList();
        newCookies.Should().Contain(c => c.Contains("access_token="));

    }

    [Fact]
    public async Task Refresh_MissingCookie_ReturnsUnauthorized()
    {
        var refreshResponse = await Client.PostAsync("/api/auth/refresh", null);

        refreshResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidRefreshToken_ReturnsUnauthorizedAndClearsCookies()
    {
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshReq.Headers.Add("Cookie", "refresh_token=lol");

        var refreshResponse = await Client.SendAsync(refreshReq);
        refreshResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        refreshResponse.Headers.Contains("Set-Cookie").Should().BeTrue();

        var cookies = refreshResponse.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("access_token=;"));
        cookies.Should().Contain(c => c.Contains("refresh_token=;"));
    }

    [Fact]
    public async Task Me_WithValidAccessToken_ReturnsUser()
    {
        var email = "jordan@gmail.com";
        var password = "Password123!";
        await SeedAuthUserAsync(email, "Jordan", password);

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var accessCookie = ExtractCookiePair(cookies, "access_token");

        var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meReq.Headers.Add("Cookie", accessCookie);

        var meResponse = await Client.SendAsync(meReq);

        meResponse.EnsureSuccessStatusCode();
        var userDto = await meResponse.Content.ReadFromJsonAsync<AuthUserDto>();
        userDto.Should().NotBeNull();
        userDto.Email.Should().Be(email);
        userDto.DisplayName.Should().Be("Jordan");

    }

    [Fact]
    public async Task Logout_AuthenticatedUser_ClearsCookiesAndInvalidatesRefreshToken()
    {
        var email = "jordan-logout@optilifts.com";
        var password = "Password123!";
        var user = await SeedAuthUserAsync(email, "Jordan", password);

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var accessCookie = ExtractCookiePair(cookies, "access_token");
        var refreshCookie = ExtractCookiePair(cookies, "refresh_token");

        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"{accessCookie}; {refreshCookie}");

        var logoutResponse = await Client.SendAsync(logoutRequest);

        logoutResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        logoutResponse.Headers.Contains("Set-Cookie").Should().BeTrue();
        var clearedCookies = logoutResponse.Headers.GetValues("Set-Cookie").ToList();
        clearedCookies.Should().Contain(c => c.Contains("access_token=;"));
        clearedCookies.Should().Contain(c => c.Contains("refresh_token=;"));

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);
        var refreshResponse = await Client.SendAsync(refreshRequest);

        refreshResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var persistedUser = await db.Users.FirstAsync(existingUser => existingUser.Id == user.Id);
        persistedUser.RefreshTokenHash.Should().BeNull();
        persistedUser.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task Me_WithInvalidAccessToken_ReturnsUnauthorized()
    {
        var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meReq.Headers.Add("Cookie", "access_token=lol");

        var meResponse = await Client.SendAsync(meReq);

        meResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClientWithGoogleAuth(IGoogleAuthService googleAuthService)
    {
        return Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGoogleAuthService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(googleAuthService);
            });
        }).CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GoogleAuth_NewUser_CreatesUserAndReturnsCookiesAndUserDto()
    {
        var googleMock = new Mock<IGoogleAuthService>();
        googleMock
            .Setup(g => g.ValidateIdTokenAsync("valid-google-id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfoDto("google-sub-integ-1", "integgoogle@example.com", "Google Integration User", "https://photo.url/pic.jpg"));

        var client = CreateClientWithGoogleAuth(googleMock.Object);

        var response = await client.PostAsJsonAsync("/api/auth/google", new { IdToken = "valid-google-id-token" });

        response.EnsureSuccessStatusCode();
        response.Headers.Contains("Set-Cookie").Should().BeTrue();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("access_token="));
        cookies.Should().Contain(c => c.Contains("refresh_token="));

        var userDto = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be("integgoogle@example.com");
        userDto.DisplayName.Should().Be("Google Integration User");

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == "google-sub-integ-1");
        userInDb.Should().NotBeNull();
        userInDb!.PasswordHash.Should().BeNull();
        userInDb.ProfileImageUrl.Should().Be("https://photo.url/pic.jpg");
    }

    [Fact]
    public async Task GoogleAuth_ExistingEmailUser_LinksGoogleIdAndAuthenticates()
    {
        var email = "linkme@example.com";
        var preseededUser = await SeedAuthUserAsync(email, "Original Preseed", "Password123!");

        var googleMock = new Mock<IGoogleAuthService>();
        googleMock
            .Setup(g => g.ValidateIdTokenAsync("valid-google-id-token-link", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfoDto("google-sub-link-99", email, "Original Preseed", null));

        var client = CreateClientWithGoogleAuth(googleMock.Object);

        var response = await client.PostAsJsonAsync("/api/auth/google", new { IdToken = "valid-google-id-token-link" });

        response.EnsureSuccessStatusCode();
        response.Headers.Contains("Set-Cookie").Should().BeTrue();

        var userDto = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(preseededUser.Id);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var updatedUser = await db.Users.FirstAsync(u => u.Id == preseededUser.Id);
        updatedUser.GoogleId.Should().Be("google-sub-link-99");
        updatedUser.PasswordHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GoogleAuth_InvalidToken_ReturnsUnauthorized()
    {
        var googleMock = new Mock<IGoogleAuthService>();
        googleMock
            .Setup(g => g.ValidateIdTokenAsync("bad-id-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidJwtException("Invalid token signature or expired"));

        var client = CreateClientWithGoogleAuth(googleMock.Object);

        var response = await client.PostAsJsonAsync("/api/auth/google", new { IdToken = "bad-id-token" });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GoogleAuth_MissingToken_ReturnsBadRequest()
    {
        var googleMock = new Mock<IGoogleAuthService>();
        var client = CreateClientWithGoogleAuth(googleMock.Object);

        var response = await client.PostAsJsonAsync("/api/auth/google", new { IdToken = "" });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}