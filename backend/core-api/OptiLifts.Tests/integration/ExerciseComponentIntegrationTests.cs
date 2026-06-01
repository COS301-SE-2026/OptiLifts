using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Exercises.GetExercises;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using Testcontainers.PostgreSql;
using Xunit;
using System.Net.Http;
using System.Text;

namespace OptiLifts.Tests.Integration;

public sealed class ExerciseComponentIntegrationTests : IClassFixture<ExercisesApiFixture>
{
    private readonly ExercisesApiFixture _fixture;

    public ExerciseComponentIntegrationTests(ExercisesApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateCustomExercise_CanBeRetrievedByGetExercises()
    {
        var user = await _fixture.SeedUserAsync("integration-exercise-1@optilifts.com", "Exercise User One");

        var client = _fixture.GetAuthenticatedClient(user);

        using var createContent = BuildCustomExerciseContent(
            name: "Custom Curl",
            mechanic: "isolation",
            equipment: "dumbbell",
            category: "Strength",
            primaryMuscles: ["Biceps"],
            secondaryMuscles: []);

        var createResponse = await client.PostAsync("/api/exercises/custom", createContent);

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateResult>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        var getResponse = await client.GetAsync("/api/exercises");
        getResponse.EnsureSuccessStatusCode();

        var exercises = await getResponse.Content.ReadFromJsonAsync<ExerciseDto[]>();
        exercises.Should().NotBeNull();
        exercises.Should().Contain(e => e.Name == "Custom Curl" && e.IsCustom && e.PrimaryMuscles.Contains("Biceps"));
    }

    [Fact]
    public async Task GetExercises_ReturnsOnlyAuthenticatedUsersExercises()
    {
        var userOne = await _fixture.SeedUserAsync("integration-exercise-2@optilifts.com", "Exercise User Two");
        var userTwo = await _fixture.SeedUserAsync("integration-exercise-3@optilifts.com", "Exercise User Three");

        var clientOne = _fixture.GetAuthenticatedClient(userOne);
        var clientTwo = _fixture.GetAuthenticatedClient(userTwo);

        using var userOneContent = BuildCustomExerciseContent(
            name: "UserOne Exercise",
            mechanic: "compound",
            equipment: "barbell",
            category: "Strength",
            primaryMuscles: ["Back"],
            secondaryMuscles: []);

        var resp1 = await clientOne.PostAsync("/api/exercises/custom", userOneContent);
        resp1.EnsureSuccessStatusCode();

        using var userTwoContent = BuildCustomExerciseContent(
            name: "UserTwo Exercise",
            mechanic: "compound",
            equipment: "barbell",
            category: "Strength",
            primaryMuscles: ["Chest"],
            secondaryMuscles: []);

        var resp2 = await clientTwo.PostAsync("/api/exercises/custom", userTwoContent);
        resp2.EnsureSuccessStatusCode();

        var getOne = await clientOne.GetAsync("/api/exercises");
        getOne.EnsureSuccessStatusCode();
        var exercisesOne = await getOne.Content.ReadFromJsonAsync<ExerciseDto[]>();

        exercisesOne.Should().NotBeNull();
        exercisesOne!.Select(e => e.Name).Should().Contain("UserOne Exercise");
        exercisesOne.Select(e => e.Name).Should().NotContain("UserTwo Exercise");
    }

    private record CreateResult(Guid Id);

    private static MultipartFormDataContent BuildCustomExerciseContent(
        string name,
        string mechanic,
        string equipment,
        string category,
        IEnumerable<string> primaryMuscles,
        IEnumerable<string> secondaryMuscles)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(name, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Name) },
            { new StringContent(mechanic, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Mechanic) },
            { new StringContent(equipment, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Equipment) },
            { new StringContent(category, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Category) },
        };

        foreach (var muscle in primaryMuscles)
        {
            content.Add(new StringContent(muscle, Encoding.UTF8), nameof(CreateCustomExerciseRequest.PrimaryMuscles));
        }

        foreach (var muscle in secondaryMuscles)
        {
            content.Add(new StringContent(muscle, Encoding.UTF8), nameof(CreateCustomExerciseRequest.SecondaryMuscles));
        }

        return content;
    }
}

public sealed class ExercisesApiFixture : IAsyncLifetime
{
    private readonly string _dbName = $"optilifts_integration_tests_{Guid.NewGuid():N}";
    private const string JwtSecret = "integration-test-secret-integration-test-secret";
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;

    public ExercisesApiFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase(_dbName)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply migrations and seed the fixture database directly to avoid concurrent migrations
        var dbOptions = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<OptiLifts.Infrastructure.Database.OptiLiftsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var db = new OptiLifts.Infrastructure.Database.OptiLiftsDbContext(dbOptions))
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
            builder.ConfigureServices(services =>
            {
                services.PostConfigureAll<JwtBearerOptions>(options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(JwtSecret));
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient GetAuthenticatedClient(Domain.Users.User user)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        var tokenService = new JwtTokenService(JwtSecret, 60);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.CreateToken(user));
        return client;
    }

    public async Task<Domain.Users.User> SeedUserAsync(string email, string displayName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var user = new Domain.Users.User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = "integration-hash"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
