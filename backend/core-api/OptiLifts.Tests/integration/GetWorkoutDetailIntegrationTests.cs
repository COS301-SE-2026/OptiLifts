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
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;
using Testcontainers.PostgreSql;

namespace OptiLifts.Tests.Integration;

public sealed class GetWorkoutDetailFixture : IAsyncLifetime
{
    public const string JwtSecret = "test_secret_key_for_integration_tests_only";

    private readonly string _dbName = $"optilifts_tests_{Guid.NewGuid():N}";
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;

    public GetWorkoutDetailFixture()
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
            EmailHash = EmailHasher.HashEmail(email),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DisplayName = "Test User"
        });

        db.Folders.Add(new Folder { Name = "Default", UserId = userId });
        await db.SaveChangesAsync();
        return userId;
    }

    public async Task<Guid> SeedExerciseAsync(string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var primaryMuscle = await db.Muscles.FirstOrDefaultAsync()
                    ?? new Muscle { Id = Guid.NewGuid(), Name = "Chest" };

        if (db.Entry(primaryMuscle).State == EntityState.Detached)
        {
            db.Muscles.Add(primaryMuscle);
        }

        var exercise = new Exercise
        {
            Name = name,
            ExerciseType = default,
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = primaryMuscle.Id
        };

        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        return exercise.Id;
    }
}

public class GetWorkoutDetailIntegrationTests : IClassFixture<GetWorkoutDetailFixture>
{
    private readonly GetWorkoutDetailFixture _fixture;

    public GetWorkoutDetailIntegrationTests(GetWorkoutDetailFixture fixture)
    {
        _fixture = fixture;
    }

    private static string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetWorkoutDetailFixture.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetWorkoutDetail_ReturnsWorkoutWithSetsFromSetsTable()
    {
        var userId = await _fixture.SeedUserAsync("detail@example.com");
        var exerciseId = await _fixture.SeedExerciseAsync("Incline Press");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        var createBody = new CreateWorkoutRequest(
            null,
            "Push Day",
            [new CreateWorkoutExerciseRequest(
                exerciseId,
                1,
                null,
                [new CreateWorkoutSetRequest("Normal", 8, 80, null, null, 1, 90)])],
            []);

        var createResp = await client.PostAsJsonAsync("/api/workouts", createBody);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        created.Should().NotBeNull();

        var detailResp = await client.GetAsync($"/api/workouts/{created!.WorkoutId}");
        detailResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResp.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("Push Day");
        detail.Exercises.Should().HaveCount(1);
        detail.Exercises[0].Name.Should().Be("Incline Press");
        detail.Exercises[0].Sets.Should().HaveCount(1);
        detail.Exercises[0].Sets[0].Type.Should().Be("Normal");
        detail.Exercises[0].Sets[0].Reps.Should().Be(8);
        detail.Exercises[0].Sets[0].Weight.Should().Be(80);
    }
}