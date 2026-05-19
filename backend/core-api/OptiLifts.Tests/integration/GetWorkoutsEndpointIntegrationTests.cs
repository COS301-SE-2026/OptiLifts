using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using Testcontainers.PostgreSql;
using Xunit;

namespace OptiLifts.Tests.Integration;

public sealed class GetWorkoutsEndpointIntegrationTests : IClassFixture<GetWorkoutsApiFixture>
{
    private readonly GetWorkoutsApiFixture _fixture;
    public GetWorkoutsEndpointIntegrationTests(GetWorkoutsApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetWorkouts_ReturnsSeededWorkoutsForAuthenticatedUser()
    {
        var user = await _fixture.SeedUserAsync("integration-user-1@optilifts.com", "Integration User One");
        await _fixture.SeedWorkoutAsync(
            user.Id,
            new DateTime(2026, 05, 19, 10, 0, 0, DateTimeKind.Utc),
            "Push Day A",
            ("Bench Press", new[] { "Chest" }),
            ("Overhead Press", new[] { "Shoulders", "Triceps" }));

        var response = await _fixture.GetAuthenticatedClient(user).GetAsync("/api/workouts");
        response.EnsureSuccessStatusCode();

        var workouts = await response.Content.ReadFromJsonAsync<WorkoutCardDto[]>();
        workouts.Should().NotBeNull();
        workouts.Should().HaveCount(1);

        var workout = workouts![0];
        workout.Name.Should().Be("Push Day A");
        workout.ExerciseCount.Should().Be(2);
        workout.ExercisePreview.Should().Equal("Bench Press", "Overhead Press");
        workout.PrimaryMuscleGroups.Should().Equal("Chest", "Shoulders", "Triceps");
        workout.CreatedAt.Should().BeCloseTo(new DateTime(2026, 05, 19, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetWorkouts_ReturnsOnlyAuthenticatedUsersWorkoutsOrderedNewestFirst()
    {
        var userOne = await _fixture.SeedUserAsync("integration-user-2@optilifts.com", "Integration User Two");
        var otherUser = await _fixture.SeedUserAsync("integration-user-3@optilifts.com", "Integration User Three");

        await _fixture.SeedWorkoutAsync(
            userOne.Id,
            new DateTime(2026, 05, 19, 9, 0, 0, DateTimeKind.Utc),
            "Old Workout",
            ("Row", new[] { "Back" }));
        await _fixture.SeedWorkoutAsync(
            userOne.Id,
            new DateTime(2026, 05, 19, 11, 0, 0, DateTimeKind.Utc),
            "New Workout",
            ("Squat", new[] { "Quadriceps", "Glutes" }));
        await _fixture.SeedWorkoutAsync(
            otherUser.Id,
            new DateTime(2026, 05, 19, 12, 0, 0, DateTimeKind.Utc),
            "Other User Workout",
            ("Bench Press", new[] { "Chest" }));

        var response = await _fixture.GetAuthenticatedClient(userOne).GetAsync("/api/workouts");

        response.EnsureSuccessStatusCode();

        var workouts = await response.Content.ReadFromJsonAsync<WorkoutCardDto[]>();
        workouts.Should().NotBeNull();
        workouts.Should().HaveCount(2);
        workouts![0].Name.Should().Be("New Workout");
        workouts[1].Name.Should().Be("Old Workout");
        workouts.Select(workout => workout.Name).Should().NotContain("Other User Workout");
    }
}

public sealed class GetWorkoutsApiFixture : IAsyncLifetime
{
    private const string JwtSecret = "integration-test-secret-integration-test-secret";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("optilifts_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("JWT_SECRET", JwtSecret);
        Environment.SetEnvironmentVariable("JWT_EXP_MINUTES", "60");

        _factory = new WebApplicationFactory<Program>();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient GetAuthenticatedClient(User user)
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

    public async Task<User> SeedUserAsync(string email, string displayName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = "integration-hash"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task SeedWorkoutAsync(
        Guid userId,
        DateTime createdAt,
        string workoutName,
        params (string ExerciseName, string[] PrimaryMuscles)[] exercises)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var folder = new Folder
        {
            UserId = userId,
            Name = $"Folder-{workoutName}",
            CreatedAt = createdAt.AddMinutes(-5)
        };

        var workout = new Workout
        {
            FolderId = folder.Id,
            Name = workoutName,
            CreatedBy = userId,
            CreatedAt = createdAt
        };

        db.Folders.Add(folder);
        db.Workouts.Add(workout);

        for (var index = 0; index < exercises.Length; index++)
        {
            var (exerciseName, primaryMuscles) = exercises[index];
            var exercise = new Exercise
            {
                Name = exerciseName,
                Category = "Strength",
                Mechanic = "compound",
                Equipment = "barbell",
                PrimaryMuscles = primaryMuscles.ToList(),
                SecondaryMuscles = new List<string>()
            };
            db.Exercises.Add(exercise);
            db.Sets.Add(new WorkoutSet
            {
                WorkoutId = workout.Id,
                ExerciseId = exercise.Id,
                Type = SetType.Normal,
                Reps = 8,
                Weight = 100,
                OrderIndex = index,
                RestTime = 90
            });
        }
        await db.SaveChangesAsync();
    }
}
