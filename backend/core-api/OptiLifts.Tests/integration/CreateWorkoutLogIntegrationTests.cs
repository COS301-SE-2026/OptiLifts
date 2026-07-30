using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class CreateWorkoutLogIntegrationTests : IntegrationTestBase
{
    public CreateWorkoutLogIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // WO = WorkOut
    [Fact]
    public async Task FinishedSession_Returns201andEntryId()
    {
        var userId = await SeedUserAsync("finish-session-1@example.com");
        var exerciseId = await SeedExerciseAsync("Bench Press");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWOBody = new CreateWorkoutRequest(null, "Push Day", [], []);
        var createWOResp = await Client.PostAsJsonAsync("/api/workouts", createWOBody);

        createWOResp.EnsureSuccessStatusCode();

        var WO = await createWOResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();

        var logId = Guid.NewGuid();
        var started = DateTime.UtcNow.AddMinutes(-45);
        var completed = DateTime.UtcNow;

        var logBody = new CreateWorkoutLogReq(
            logId,
            null,
            null,
            started,
            completed,
            [new CreateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                [new CreateWorkoutLogSetReq(null, "Normal", 8, 80f, null, null, 90, 8f, 1, 0)])]);

        var resp = await Client.PostAsJsonAsync($"/api/workouts/{WO!.WorkoutId}/logs", logBody);
        var res = await resp.Content.ReadFromJsonAsync<CreateWorkoutLogRes>();

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        res.Should().NotBeNull();
        res!.LogId.Should().Be(logId);
        res.EntryId.Should().NotBeEmpty();
        res.AlreadyExisted.Should().BeFalse();
    }

    [Fact]
    public async Task SameLogIdTwice_ToldAlreadyExist()
    {
        var userId = await SeedUserAsync("finish-session-2@example.com");
        var exerciseId = await SeedExerciseAsync("Squat");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWOBody = new CreateWorkoutRequest(null, "Leg Day", [], []);
        var createWOresp = await Client.PostAsJsonAsync("/api/workouts", createWOBody);
        var WO = await createWOresp.Content.ReadFromJsonAsync<CreateWorkoutResult>();

        var logId = Guid.NewGuid();
        var logBody = new CreateWorkoutLogReq(
            logId, null, null, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow,
            [new CreateWorkoutLogExerciseReq(exerciseId, null, 1, 0,
                [new CreateWorkoutLogSetReq(null, "Normal", 10, 60f, null, null, 60, 7f, 1, 0)])]);

        var resp1 = await Client.PostAsJsonAsync($"/api/workouts/{WO!.WorkoutId}/logs", logBody);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);
        var res1 = await resp1.Content.ReadFromJsonAsync<CreateWorkoutLogRes>();

        var resp2 = await Client.PostAsJsonAsync($"/api/workouts/{WO.WorkoutId}/logs", logBody);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var res2 = await resp2.Content.ReadFromJsonAsync<CreateWorkoutLogRes>();

        res2!.EntryId.Should().Be(res1!.EntryId);
        res2.AlreadyExisted.Should().BeTrue();
    }
}
