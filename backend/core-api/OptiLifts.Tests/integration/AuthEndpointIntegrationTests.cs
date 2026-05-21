using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Domain.Users;
using Testcontainers.PostgreSql;

namespace OptiLifts.Tests.Integration;

public sealed class AuthEndpointIntegrationTests : IClassFixture<AuthApiFixture>
{
    private readonly AuthApiFixture _fixture;
    public AuthEndpointIntegrationTests(AuthApiFixture fixture)
    {
        _fixture = fixture;
    }

    private record RegisterRequest(string DisplayName, string Email, string Password);
    private record LoginRequest(string Email, string Password);
    private record AuthUserDto(Guid Id, string DisplayName, string Email, DateTime CreatedAt);
    private record AuthResponseDto(string Token, AuthUserDto User);

    [Fact]
    public async Task Register_Succeeds_ReturnsTokenAndUser()
    {
        var client = _fixture.CreateClient();

        var request = new RegisterRequest("Jordan", "jordanRegister@gmail.com", "P@ssw0rd!");
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        dto.Should().NotBeNull();
        dto.Token.Should().NotBeNullOrWhiteSpace();
        dto.User.Email.Should().Be(request.Email);
        dto.User.DisplayName.Should().Be(request.DisplayName);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _fixture.CreateClient();
        var email = "jordanDuplicate@gmail.com";

        var first = new RegisterRequest("First", email, "Password123!");
        var second = new RegisterRequest("Second", email, "Password2234!");

        var r1 = await client.PostAsJsonAsync("/api/auth/register", first);
        r1.EnsureSuccessStatusCode();

        var r2 = await client.PostAsJsonAsync("/api/auth/register", second);
        r2.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_Succeeds_ReturnsTokenAndUser()
    {
        var email = "jordanLogin@optilifts.com";
        var password = "Password123!";
        await _fixture.SeedUserAsync(email, "Jordan", password);

        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        dto.Should().NotBeNull();
        dto.Token.Should().NotBeNullOrWhiteSpace();
        dto.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var email = "jordanBadLogin@gmail.com";
        var password = "Password123!";
        await _fixture.SeedUserAsync(email, "Jordan", password);

        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword"));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_MissingFields_ReturnsBadRequest()
    {
        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { DisplayName = "", Email = "", Password = "" });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}

public sealed class AuthApiFixture : IAsyncLifetime
{
    private const string JwtSecret = "integration-test-secret-integration-test-secret";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("optilifts_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        //migrations and seeding
        var dbOptions = new DbContextOptionsBuilder<OptiLifts.Infrastructure.Database.OptiLiftsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var db = new OptiLifts.Infrastructure.Database.OptiLiftsDbContext(dbOptions))
        {
            await db.Database.MigrateAsync();
            await OptiLifts.Infrastructure.Database.Seeders.DatabaseSeeder.SeedAsync(db);
        }

        //env setup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", JwtSecret);
        Environment.SetEnvironmentVariable("JWT_EXP_MINUTES", "60");
        Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString());

        //factory specific setup
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.PostConfigureAll<JwtBearerOptions>(options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
                });
            });
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["POSTGRES_CONNECTION_STRING"] = _postgres.GetConnectionString(),
                    ["JWT_SECRET"] = JwtSecret,
                    ["JWT_EXP_MINUTES"] = "60",
                    ["FRONTEND_ORIGIN"] = "localhost:5173",
                    ["RUN_MIGRATIONS"] = "false"
                };
                config.AddInMemoryCollection(dict!);
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });
    }

    public async Task<User> SeedUserAsync(string email, string displayName, string password)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = hasher.Hash(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
