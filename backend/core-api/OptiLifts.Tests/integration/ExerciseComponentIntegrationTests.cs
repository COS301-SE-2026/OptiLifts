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
using OptiLifts.Application.Exercises.GetExercises;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using Testcontainers.PostgreSql;
using Xunit;

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

        var createResponse = await client.PostAsJsonAsync("/api/exercises/custom", new
        {
            Name = "Custom Curl",
            Mechanic = "isolation",
            Equipment = "dumbbell",
            Category = "Strength",
            PrimaryMuscles = new[] { "Biceps" },
            SecondaryMuscles = new string[] { }
        });

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

        var resp1 = await clientOne.PostAsJsonAsync("/api/exercises/custom", new
        {
            Name = "UserOne Exercise",
            Mechanic = "compound",
            Equipment = "barbell",
            Category = "Strength",
            PrimaryMuscles = new[] { "Back" },
            SecondaryMuscles = new string[] { }
        });
        resp1.EnsureSuccessStatusCode();

        var resp2 = await clientTwo.PostAsJsonAsync("/api/exercises/custom", new
        {
            Name = "UserTwo Exercise",
            Mechanic = "compound",
            Equipment = "barbell",
            Category = "Strength",
            PrimaryMuscles = new[] { "Chest" },
            SecondaryMuscles = new string[] { }
        });
        resp2.EnsureSuccessStatusCode();

        var getOne = await clientOne.GetAsync("/api/exercises");
        getOne.EnsureSuccessStatusCode();
        var exercisesOne = await getOne.Content.ReadFromJsonAsync<ExerciseDto[]>();

        exercisesOne.Should().NotBeNull();
        exercisesOne!.Select(e => e.Name).Should().Contain("UserOne Exercise");
        exercisesOne.Select(e => e.Name).Should().NotContain("UserTwo Exercise");
    }

    private record CreateResult(Guid Id);
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

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("JWT_SECRET", JwtSecret);
        Environment.SetEnvironmentVariable("JWT_EXP_MINUTES", "60");

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
            builder.ConfigureServices(services =>
            {
                services.PostConfigureAll<JwtBearerOptions>(options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(JwtSecret));
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
