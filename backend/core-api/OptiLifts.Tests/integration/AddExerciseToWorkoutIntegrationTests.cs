using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class AddExerciseToWorkoutIntegrationTests : IntegrationTestBase
{
    public AddExerciseToWorkoutIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
        //to execute base constructor
    }

    public async Task<Guid> SeedExerciseAsync(string name)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
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

    public async Task<bool> HasSetAsync(Guid workoutId, Guid exerciseId)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var workoutExercise = await db.WorkoutExercises
            .AsNoTracking()
            .FirstOrDefaultAsync(we => we.WorkoutId == workoutId && we.ExerciseId == exerciseId);

        if (workoutExercise == null)
        {
            return false;
        }

        return await db.Sets.AnyAsync(s => s.WorkoutExerciseId == workoutExercise.Id);
    }

    [Fact]
    public async Task AddExerciseToWorkout_ReturnsNoContent_AndCreatesSet()
    {
        var userId = await SeedUserAsync("addex@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createBody = new CreateWorkoutRequest(null, "Chest Day", [], []);
        var createResp = await Client.PostAsJsonAsync("/api/workouts", createBody);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        created.Should().NotBeNull();
        var workoutId = created!.WorkoutId;

        var exerciseId = await SeedExerciseAsync("Integration Press");

        var addResp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/exercises", new { ExerciseId = exerciseId });
        addResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var exists = await HasSetAsync(workoutId, exerciseId);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AddExerciseToWorkout_ReturnsNotFound_WhenExerciseMissing()
    {
        var userId = await SeedUserAsync("addex2@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createBody = new CreateWorkoutRequest(null, "Back Day", [], []);
        var createResp = await Client.PostAsJsonAsync("/api/workouts", createBody);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        created.Should().NotBeNull();
        var workoutId = created!.WorkoutId;

        var addResp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/exercises", new { ExerciseId = Guid.NewGuid() });
        addResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}