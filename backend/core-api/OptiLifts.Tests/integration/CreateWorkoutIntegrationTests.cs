using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class CreateWorkoutIntegrationTests : IntegrationTestBase
{
    public CreateWorkoutIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PostWorkout_Returns201_WithValidToken()
    {
        var userId = await SeedUserAsync("post1@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var body = new CreateWorkoutRequest(null, "Push Day A", [], []);
        var response = await Client.PostAsJsonAsync("/api/workouts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Push Day A");
        result.WorkoutId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostWorkout_Returns401_WhenNoToken()
    {
        var body = new CreateWorkoutRequest(null, "Push Day", [], []);
        var response = await Client.PostAsJsonAsync("/api/workouts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostThenGetWorkout_ReturnsWorkoutInList()
    {
        var userId = await SeedUserAsync("getpost@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var body = new CreateWorkoutRequest(null, "Leg Day", [], []);
        await Client.PostAsJsonAsync("/api/workouts", body);
        var response = await Client.GetAsync("/api/workouts");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutCardDto>>();
        workouts.Should().NotBeNull();
        workouts!.Should().HaveCount(1);
        workouts[0].Name.Should().Be("Leg Day");
    }
}
