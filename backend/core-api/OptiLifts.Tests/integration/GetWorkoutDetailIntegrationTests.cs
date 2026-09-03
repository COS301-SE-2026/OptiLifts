using System.Linq;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class GetWorkoutDetailIntegrationTests : IntegrationTestBase
{
    public GetWorkoutDetailIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetWorkoutDetail_ReturnsWorkoutWithSetsFromSetsTable()
    {
        var userId = await SeedUserAsync("detail@example.com");
        var exerciseId = await SeedExerciseAsync("Incline Press");

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createBody = new CreateWorkoutRequest(
            null,
            "Push Day",
            [new CreateWorkoutExerciseRequest(
                exerciseId,
                1,
                null,
                [new CreateWorkoutSetRequest("Normal", 8, 80, null, null, 1, 90)])],
            []);

        var createResp = await Client.PostAsJsonAsync("/api/workouts", createBody);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        created.Should().NotBeNull();

        var detailResp = await Client.GetAsync($"/api/workouts/{created.WorkoutId}");
        detailResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResp.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        detail.Should().NotBeNull();
        detail.Name.Should().Be("Push Day");
        detail.Exercises.Should().HaveCount(1);
        detail.Exercises[0].Name.Should().Be("Incline Press");
        detail.Exercises[0].Sets.Should().HaveCount(1);
        detail.Exercises[0].Sets[0].Type.Should().Be("Normal");
        detail.Exercises[0].Sets[0].Reps.Should().Be(8);
        detail.Exercises[0].Sets[0].Weight.Should().Be(80);
    }

    [Fact]
    public async Task GetWorkoutDetail_TimeConstrained_CalculatesSetsAndTrimsTime()
    {
        var userId = await SeedUserAsync("detail_time@example.com");
        var exerciseId = await SeedExerciseAsync("Bench Press");

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createBody = new CreateWorkoutRequest(
            null,
            "Push Day Time",
            [new CreateWorkoutExerciseRequest(
                exerciseId,
                1,
                null,
                [
                    new CreateWorkoutSetRequest("Normal", 10, 80, null, null, 1, 300),
                    new CreateWorkoutSetRequest("Normal", 10, 80, null, null, 2, 300),
                    new CreateWorkoutSetRequest("Normal", 10, 80, null, null, 3, 300)
                ])],
            []);

        var createResp = await Client.PostAsJsonAsync("/api/workouts", createBody);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        created.Should().NotBeNull();

        var detailResp = await Client.GetAsync($"/api/workouts/{created.WorkoutId}?isTimeConstrained=true&timeBudgetMinutes=5");
        detailResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResp.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        detail.Should().NotBeNull();
        detail.Name.Should().Be("Push Day Time");
        detail.Exercises.Should().HaveCount(1);

        var totalSets = detail.Exercises[0].Sets.Length;
        totalSets.Should().BeGreaterThan(0);

        var totalTime = 0;
        foreach (var s in detail.Exercises[0].Sets)
        {
            totalTime += ((s.Reps ?? 10) * 4) + s.RestTime;
        }
        totalTime.Should().BeLessThanOrEqualTo(5 * 60 + 60);
    }

}