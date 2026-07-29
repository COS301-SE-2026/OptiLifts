using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
}