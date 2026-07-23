using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkoutLogDetail;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class GetWorkoutLogDetailIntegrationTests : IntegrationTestBase
{
    public GetWorkoutLogDetailIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetWorkoutLogDetail_ReturnsLogWithSets()
    {
        var userId = await SeedUserAsync("logdetail@example.com");
        var exerciseId = await SeedExerciseAsync("Bench Press");

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWorkoutBody = new CreateWorkoutRequest(null, "Push Day Log", [new CreateWorkoutExerciseRequest(exerciseId, 1, null, [new CreateWorkoutSetRequest("Normal", 8, 80, null, null, 1, 90)])], []);

        var createWorkoutResp = await Client.PostAsJsonAsync("/api/workouts", createWorkoutBody);
        createWorkoutResp.EnsureSuccessStatusCode();
        var createdWorkout = await createWorkoutResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        createdWorkout.Should().NotBeNull();
        var workoutId = createdWorkout!.WorkoutId;

        var logId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddHours(-1);
        var completedAt = DateTime.UtcNow;
        var createLogBody = new CreateWorkoutLogReq(logId, null, "Felt great", startedAt, completedAt, [new CreateWorkoutLogExerciseReq(exerciseId, null, 1, 1, [new CreateWorkoutLogSetReq(null, "Normal", 8, 85f, null, null, 90, 8.5f, 1, 1)])]);

        var createLogResp = await Client.PostAsJsonAsync($"/api/workouts/{workoutId}/logs", createLogBody);
        createLogResp.EnsureSuccessStatusCode();

        var detailResp = await Client.GetAsync($"/api/workouts/{workoutId}/logs/{logId}");
        detailResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResp.Content.ReadFromJsonAsync<WorkoutLogDetailDto>();
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("Push Day Log");
        detail.Duration.Should().NotBeNull();
        detail.Exercises.Should().HaveCount(1);
        detail.Exercises[0].Name.Should().Be("Bench Press");
        detail.Exercises[0].Sets.Should().HaveCount(1);
        detail.Exercises[0].Sets[0].Type.Should().Be("Normal");
        detail.Exercises[0].Sets[0].Reps.Should().Be(8);
        detail.Exercises[0].Sets[0].Weight.Should().Be(85f);
        detail.Exercises[0].Sets[0].Rpe.Should().Be(8.5f);
    }
}