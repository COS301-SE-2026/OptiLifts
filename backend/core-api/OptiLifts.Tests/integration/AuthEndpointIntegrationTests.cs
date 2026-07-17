using System.Net.Http.Json;
using FluentAssertions;
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

    [Fact]
    public async Task Register_Succeeds_ReturnsTokenAndUser()
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
    public async Task Login_Succeeds_ReturnsTokenAndUser()
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
}