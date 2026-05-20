using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using Testcontainers.PostgreSql;

namespace OptiLifts.Tests.Integration;

//integration test 1: POST /api/workouts returns 201 with a real postgres db
//integration test 2: POST /api/workouts without token returns 401
//integration test 3: POST then GET returns workout in list (full end-to-end)

public class WorkoutsIntegrationTests : IAsyncLifetime
{
    private const string TestJwtSecret = "test_secret_key_for_integration_tests_only";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("optilifts_test")
        .WithUsername("postgres")
        .WithPassword("test")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Environment.SetEnvironmentVariable("JWT_SECRET", TestJwtSecret);
        Environment.SetEnvironmentVariable("JWT_EXP_MINUTES", "60");
        Environment.SetEnvironmentVariable("POSTGRES_HOST", "localhost");
        Environment.SetEnvironmentVariable("POSTGRES_PORT", "5432");
        Environment.SetEnvironmentVariable("POSTGRES_DB", "optilifts_test");
        Environment.SetEnvironmentVariable("POSTGRES_USER", "postgres");
        Environment.SetEnvironmentVariable("POSTGRES_PASSWORD", "test");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<OptiLiftsDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<OptiLiftsDbContext>(options =>
                    options.UseNpgsql(_postgres.GetConnectionString()));

                services.PostConfigureAll<JwtBearerOptions>(options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
                });
            });
        });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.StopAsync();
    }

    private string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<(Guid userId, Guid folderId)> SeedUserAndFolder(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DisplayName = "Test User"
        });

        var folder = new Folder { Name = "Default", UserId = userId };
        db.Folders.Add(folder);
        await db.SaveChangesAsync();
        folder = await db.Folders.FirstAsync(f => f.UserId == userId);
        return (userId, folder.Id);
    }

    [Fact]
    public async Task PostWorkout_Returns201_WithValidToken()
    {
        var (userId, _) = await SeedUserAndFolder("post1@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        var body = new CreateWorkoutRequest(null, "Push Day A", 1, []);
        var response = await _client.PostAsJsonAsync("/api/workouts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Push Day A");
        result.WorkoutId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostWorkout_Returns401_WhenNoToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var body = new CreateWorkoutRequest(null, "Push Day", null, []);
        var response = await _client.PostAsJsonAsync("/api/workouts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostThenGetWorkout_ReturnsWorkoutInList()
    {
        var (userId, _) = await SeedUserAndFolder("getpost@example.com");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        var body = new CreateWorkoutRequest(null, "Leg Day", 3, []);
        await _client.PostAsJsonAsync("/api/workouts", body);

        var response = await _client.GetAsync("/api/workouts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutCardDto>>();
        workouts.Should().NotBeNull();
        workouts!.Should().HaveCount(1);
        workouts[0].Name.Should().Be("Leg Day");
    }
}
