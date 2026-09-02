using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class ProgressiveOverloadIntegrationTests : IntegrationTestBase
{
    public ProgressiveOverloadIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task WeightedExercise_FourConsistentSessions_GeneratesEstimationOnWorkoutDetail()
    {
        var userId = await SeedUserAsync("po-weighted@example.com");
        var exerciseId = await SeedExerciseAsync("Bench Press", ExerciseType.WeightReps, mechanic: null, equipment: "barbell");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var workoutId = await CreateWorkoutWithExerciseAsync("Push Day", exerciseId);

        for (int i = 0; i < 4; i++)
        {
            await LogSessionAsync(workoutId, exerciseId, weight: 100f, reps: 10, daysAgo: (3 - i) * 7);
        }

        var detail = await GetWorkoutDetailAsync(workoutId);
        var exercise = detail!.Exercises.Single(e => e.ExerciseId == exerciseId);

        exercise.Estimation.Should().NotBeNull();
        exercise.Estimation.Weight.Should().Be(100f);
        //flat history has no trend, so the minimum-growth floor pushes reps up by one instead of stalling.
        exercise.Estimation.Reps.Should().Be(11);
    }

    [Fact]
    public async Task BodyweightExercise_FourConsistentSessions_AlwaysRecommendsOneMoreRepThanPreviousSession()
    {
        var userId = await SeedUserAsync("po-bodyweight@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
        await SetUserBodyweightAsync(80f);
        var exerciseId = await SeedExerciseAsync("Pull-up", ExerciseType.BodyweightReps);

        var workoutId = await CreateWorkoutWithExerciseAsync("Pull Day", exerciseId);

        for (int i = 0; i < 4; i++)
        {
            await LogSessionAsync(workoutId, exerciseId, weight: 0f, reps: 10, daysAgo: (3 - i) * 7);
        }

        var detail = await GetWorkoutDetailAsync(workoutId);
        var exercise = detail!.Exercises.Single(e => e.ExerciseId == exerciseId);

        exercise.Estimation.Should().NotBeNull();
        exercise.Estimation.Weight.Should().BeNull();
        exercise.Estimation.Reps.Should().Be(11);
    }

    [Fact]
    public async Task WeightedExercise_FewerThanFourSessions_NoEstimationIsGenerated()
    {
        var userId = await SeedUserAsync("po-insufficient@example.com");
        var exerciseId = await SeedExerciseAsync("Deadlift");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var workoutId = await CreateWorkoutWithExerciseAsync("Pull Day", exerciseId);

        for (int i = 0; i < 3; i++)
        {
            await LogSessionAsync(workoutId, exerciseId, weight: 100f, reps: 10, daysAgo: (2 - i) * 7);
        }

        var detail = await GetWorkoutDetailAsync(workoutId);
        var exercise = detail!.Exercises.Single(e => e.ExerciseId == exerciseId);

        exercise.Estimation.Should().BeNull();
    }

    [Fact]
    public async Task WeightedExercise_GapGreaterThanFourteenDays_PreventsEstimation()
    {
        var userId = await SeedUserAsync("po-gap@example.com");
        var exerciseId = await SeedExerciseAsync("Overhead Press");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var workoutId = await CreateWorkoutWithExerciseAsync("Push Day", exerciseId);

        var daysAgo = new[] { 30, 14, 7, 0 };
        foreach (var offset in daysAgo)
        {
            await LogSessionAsync(workoutId, exerciseId, weight: 100f, reps: 10, daysAgo: offset);
        }

        var detail = await GetWorkoutDetailAsync(workoutId);
        var exercise = detail!.Exercises.Single(e => e.ExerciseId == exerciseId);

        exercise.Estimation.Should().BeNull();
    }

    private async Task<Guid> CreateWorkoutWithExerciseAsync(string name, Guid exerciseId)
    {
        var createResp = await Client.PostAsJsonAsync("/api/workouts", new CreateWorkoutRequest(
            null,
            name,
            [new CreateWorkoutExerciseRequest(exerciseId, 1, null, [])],
            []));
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        return created!.WorkoutId;
    }

    private async Task LogSessionAsync(Guid workoutId, Guid exerciseId, float weight, int reps, int daysAgo)
    {
        var started = DateTime.UtcNow.AddDays(-daysAgo).AddHours(-1);
        var completed = DateTime.UtcNow.AddDays(-daysAgo);

        var logBody = new CreateWorkoutLogReq(
            Guid.NewGuid(),
            null,
            null,
            started,
            completed,
            [new CreateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                [new CreateWorkoutLogSetReq(null, "Normal", reps, weight, null, null, 90, 8f, 1, 0)])]);

        var resp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/logs", logBody);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<WorkoutDetailDto?> GetWorkoutDetailAsync(Guid workoutId)
    {
        var resp = await Client.GetAsync($"/api/workouts/{workoutId}");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkoutDetailDto>();
    }

    private async Task SetUserBodyweightAsync(float weight)
    {
        var request = JsonContent.Create(new
        {
            DisplayName = "Test User",
            Bio = (string?)null,
            Sex = (string?)null,
            DateOfBirth = (DateTime?)null,
            Weight = weight,
            Height = (double?)null
        });

        var resp = await Client.PatchAsync("/api/users/me/profileDetails", request);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<Guid> SeedExerciseAsync(string name, ExerciseType exerciseType, string? mechanic = null, string? equipment = "None")
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var muscle = new Muscle { Id = Guid.NewGuid(), Name = $"Muscle {Guid.NewGuid()}" };
        var exercise = new Exercise
        {
            Name = name,
            ExerciseType = exerciseType,
            Mechanic = mechanic,
            Equipment = equipment,
            PrimaryMuscleId = muscle.Id
        };

        db.Muscles.Add(muscle);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        return exercise.Id;
    }
}
