using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkoutLogDetail;
using OptiLifts.Application.Workouts.UpdateWorkoutLog;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class UpdateWorkoutLogIntegrationTests : IntegrationTestBase
{
    public UpdateWorkoutLogIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PutWorkoutLog_Returns200Ok_AndUpdatesPastWorkoutDetails()
    {
        // Arrange
        var userId = await SeedUserAsync("edit-log-user1@example.com");
        var exerciseId = await SeedExerciseAsync("Bench Press");

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWorkoutBody = new CreateWorkoutRequest(null, "Push Day", [], []);
        var createWorkoutResp = await Client.PostAsJsonAsync("/api/workouts", createWorkoutBody);
        createWorkoutResp.EnsureSuccessStatusCode();
        var workout = await createWorkoutResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        workout.Should().NotBeNull();
        var workoutId = workout!.WorkoutId;

        var logId = Guid.NewGuid();
        var initialStart = DateTime.UtcNow.AddHours(-2);
        var initialComplete = DateTime.UtcNow.AddHours(-1);

        var createLogReq = new CreateWorkoutLogReq(
            logId,
            null,
            "Original notes",
            initialStart,
            initialComplete,
            [
                new CreateWorkoutLogExerciseReq(
                    exerciseId,
                    null,
                    1,
                    0,
                    [new CreateWorkoutLogSetReq(null, "Normal", 8, 60f, null, null, 90, 7.0f, 1, 0)])
            ]);

        var createLogResp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/logs", createLogReq);
        createLogResp.EnsureSuccessStatusCode();

        var updatedStart = DateTime.UtcNow.AddHours(-3);
        var updatedComplete = DateTime.UtcNow.AddHours(-1.5);

        var updateLogReq = new UpdateWorkoutLogReq(
            "Updated workout session - felt strong",
            updatedStart,
            updatedComplete,
            [
                new UpdateWorkoutLogExerciseReq(
                    exerciseId,
                    null,
                    1,
                    0,
                    [new UpdateWorkoutLogSetReq(null, "Normal", 10, 75f, null, null, 120, 8.5f, 1, 0)])
            ]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/logs/{logId}", updateLogReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await Client.GetAsync($"/api/workouts/{workoutId}/logs/{logId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getResp.Content.ReadFromJsonAsync<WorkoutLogDetailDto>();
        detail.Should().NotBeNull();
        detail!.LogId.Should().Be(logId);
        detail.Exercises.Should().HaveCount(1);
        detail.Exercises[0].Name.Should().Be("Bench Press");
        detail.Exercises[0].Sets.Should().HaveCount(1);
        detail.Exercises[0].Sets[0].Reps.Should().Be(10);
        detail.Exercises[0].Sets[0].Weight.Should().Be(75f);
        detail.Exercises[0].Sets[0].Rpe.Should().Be(8.5f);
        detail.Exercises[0].Sets[0].RestTime.Should().Be(120);
    }

    [Fact]
    public async Task PutWorkoutLog_UpdatesExercisePrs_WhenWeightIsIncreased()
    {
        // Arrange
        var userId = await SeedUserAsync("edit-log-pr@example.com");
        var exerciseId = await SeedExerciseAsync("Deadlift");

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWorkoutResp = await Client.PostAsJsonAsync("/api/workouts", new CreateWorkoutRequest(null, "Pull Day", [], []));
        createWorkoutResp.EnsureSuccessStatusCode();
        var workout = await createWorkoutResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        var workoutId = workout!.WorkoutId;

        var logId = Guid.NewGuid();
        var createLogReq = new CreateWorkoutLogReq(
            logId, null, null, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow,
            [
                new CreateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                    [new CreateWorkoutLogSetReq(null, "Normal", 5, 100f, null, null, 180, 8f, 1, 0)])
            ]);
        var createLogResp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/logs", createLogReq);
        createLogResp.EnsureSuccessStatusCode();

        var updateLogReq = new UpdateWorkoutLogReq(
            "New PR achieved",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            [
                new UpdateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                    [new UpdateWorkoutLogSetReq(null, "Normal", 5, 140f, null, null, 180, 9.5f, 1, 0)])
            ]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/logs/{logId}", updateLogReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var maxWeightPr = await db.ExercisePrs
            .FirstOrDefaultAsync(pr => pr.UserId == userId && pr.ExerciseId == exerciseId && pr.PrType == ExercisePrType.MaxWeight);

        maxWeightPr.Should().NotBeNull();
        maxWeightPr!.PrValue.Should().Be(140f);
        maxWeightPr.AchievedWeight.Should().Be(140f);
    }

    [Fact]
    public async Task PutWorkoutLog_RemovesAndReplacesSets_WhenSetCountIsModified()
    {
        // Arrange
        var userId = await SeedUserAsync("edit-log-sets@example.com");
        var exerciseId = await SeedExerciseAsync("Squat");

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWorkoutResp = await Client.PostAsJsonAsync("/api/workouts", new CreateWorkoutRequest(null, "Leg Day", [], []));
        createWorkoutResp.EnsureSuccessStatusCode();
        var workout = await createWorkoutResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        var workoutId = workout!.WorkoutId;

        var logId = Guid.NewGuid();
        var createLogReq = new CreateWorkoutLogReq(
            logId, null, "3 sets initially", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow,
            [
                new CreateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                    [
                        new CreateWorkoutLogSetReq(null, "Normal", 10, 60f, null, null, 60, 7f, 1, 0),
                        new CreateWorkoutLogSetReq(null, "Normal", 10, 70f, null, null, 60, 8f, 2, 0),
                        new CreateWorkoutLogSetReq(null, "Normal", 10, 80f, null, null, 60, 9f, 3, 0)
                    ])
            ]);
        var createLogResp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/logs", createLogReq);
        createLogResp.EnsureSuccessStatusCode();

        // Reduce from 3 sets to 1 set with 90kg
        var updateLogReq = new UpdateWorkoutLogReq(
            "Reduced to 1 heavy set",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            [
                new UpdateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                    [new UpdateWorkoutLogSetReq(null, "Normal", 5, 90f, null, null, 120, 9.5f, 1, 0)])
            ]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/logs/{logId}", updateLogReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await Client.GetAsync($"/api/workouts/{workoutId}/logs/{logId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getResp.Content.ReadFromJsonAsync<WorkoutLogDetailDto>();
        detail.Should().NotBeNull();
        detail!.Exercises.Should().HaveCount(1);
        detail.Exercises[0].Sets.Should().HaveCount(1);
        detail.Exercises[0].Sets[0].Weight.Should().Be(90f);
        detail.Exercises[0].Sets[0].Reps.Should().Be(5);
    }

    [Fact]
    public async Task PutWorkoutLog_Returns404NotFound_WhenLogDoesNotExist()
    {
        // Arrange
        var userId = await SeedUserAsync("edit-log-404@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Upper Body");

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var nonExistentLogId = Guid.NewGuid();
        var updateLogReq = new UpdateWorkoutLogReq(
            "Some notes",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            []
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/logs/{nonExistentLogId}", updateLogReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutWorkoutLog_Returns404NotFound_WhenWorkoutLogNotOwnedByUser()
    {
        // Arrange
        var ownerId = await SeedUserAsync("owner-log@example.com");
        var otherUserId = await SeedUserAsync("other-user-log@example.com");
        var exerciseId = await SeedExerciseAsync("Overhead Press");

        // Owner creates workout and log
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(ownerId)}");
        var createWorkoutResp = await Client.PostAsJsonAsync("/api/workouts", new CreateWorkoutRequest(null, "Shoulders", [], []));
        createWorkoutResp.EnsureSuccessStatusCode();
        var workout = await createWorkoutResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        var workoutId = workout!.WorkoutId;

        var logId = Guid.NewGuid();
        var createLogReq = new CreateWorkoutLogReq(
            logId, null, "Owner log", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow,
            [
                new CreateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                    [new CreateWorkoutLogSetReq(null, "Normal", 8, 40f, null, null, 60, 7f, 1, 0)])
            ]);
        var createLogResp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/logs", createLogReq);
        createLogResp.EnsureSuccessStatusCode();

        // Switch to non-owner user
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(otherUserId)}");

        var updateLogReq = new UpdateWorkoutLogReq(
            "Tampered notes",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            []
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/logs/{logId}", updateLogReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutWorkoutLog_Returns401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var workoutId = Guid.NewGuid();
        var logId = Guid.NewGuid();
        var updateLogReq = new UpdateWorkoutLogReq("No auth", DateTime.UtcNow, DateTime.UtcNow, []);

        // Ensure no auth cookie set
        Client.DefaultRequestHeaders.Remove("Cookie");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/logs/{logId}", updateLogReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
