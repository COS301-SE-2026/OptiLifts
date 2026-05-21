using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
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

public sealed class CreateWorkoutFixture : IAsyncLifetime
{
    public const string JwtSecret = "test_secret_key_for_integration_tests_only";

    private readonly string _dbName = $"optilifts_tests_{Guid.NewGuid():N}";
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;

    public CreateWorkoutFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase(_dbName)
            .WithUsername("postgres")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var dbOptions = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var db = new OptiLiftsDbContext(dbOptions))
        {
            await db.Database.MigrateAsync();
            await OptiLifts.Infrastructure.Database.Seeders.DatabaseSeeder.SeedAsync(db);
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString() + ";Pooling=false");
            builder.UseSetting("JWT_SECRET", JwtSecret);
            builder.UseSetting("JWT_EXP_MINUTES", "60");
            builder.UseSetting("FRONTEND_ORIGIN", "localhost:5173");
            builder.UseSetting("RUN_MIGRATIONS", "false");
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public async Task<Guid> SeedUserAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DisplayName = "Test User"
        });

        db.Folders.Add(new Folder { Name = "Default", UserId = userId });
        await db.SaveChangesAsync();
        return userId;
    }
}

public class CreateWorkoutIntegrationTests : IClassFixture<CreateWorkoutFixture>
{
    private readonly CreateWorkoutFixture _fixture;

    public CreateWorkoutIntegrationTests(CreateWorkoutFixture fixture)
    {
        _fixture = fixture;
    }

    private string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CreateWorkoutFixture.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task PostWorkout_Returns201_WithValidToken()
    {
        var userId = await _fixture.SeedUserAsync("post1@example.com");
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        var body = new CreateWorkoutRequest(null, "Push Day A", 1, []);
        var response = await client.PostAsJsonAsync("/api/workouts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Push Day A");
        result.WorkoutId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostWorkout_Returns401_WhenNoToken()
    {
        var client = _fixture.CreateClient();
        var body = new CreateWorkoutRequest(null, "Push Day", null, []);
        var response = await client.PostAsJsonAsync("/api/workouts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostThenGetWorkout_ReturnsWorkoutInList()
    {
        var userId = await _fixture.SeedUserAsync("getpost@example.com");
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        var body = new CreateWorkoutRequest(null, "Leg Day", 3, []);
        await client.PostAsJsonAsync("/api/workouts", body);

        var response = await client.GetAsync("/api/workouts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutCardDto>>();
        workouts.Should().NotBeNull();
        workouts!.Should().HaveCount(1);
        workouts[0].Name.Should().Be("Leg Day");
    }
}
