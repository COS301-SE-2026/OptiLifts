//deleting+duplicating workotu
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Tests.Integration.IntegrationDb;
using OptiLifts.Application.Workouts.DuplicateWorkout;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class WorkoutManagementIntegrationTests : IntegrationTestBase
{
    public WorkoutManagementIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task DuplicateWorkout_CreatesNewDuplicate_ForUser()
    {
        var userId = await SeedUserAsync("dupe-workout-1@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Pull day");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
        
        //act
        var response = await Client.PostAsync($"/api/workouts/{workoutId}/duplicate", null);
        //assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<DuplicateWorkoutResult>();
        result.Should().NotBeNull();
        result!.WorkoutId.Should().NotBeEmpty();
        result.WorkoutId.Should().NotBe(workoutId);
        result.Name.Should().Contain("Pull day");

        var getdetails = await Client.GetAsync($"/api/workouts/{result.WorkoutId}");
        getdetails.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getdetails.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        detail.Should().NotBeNull();
        detail!.Name.Should().Be(result.Name);
    }

    [Fact]
    public async Task DuplicateWorkout_Returns404_WhenWorkoutNotowned()
    {
        var userId = await SeedUserAsync("dupe-workout-1@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Pull day");
        var notownderId = await SeedUserAsync("notownder@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(notownderId)}");
        
        //act
        var response = await Client.PostAsync($"/api/workouts/{workoutId}/duplicate", null);
        //assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWorkout_SoftDeletesWorkout_NoRetrieval()
    {
        var userId = await SeedUserAsync("dupe-workout-1@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Pull day");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var initialreq = await Client.GetAsync($"/api/workouts/{workoutId}");
        initialreq.StatusCode.Should().Be(HttpStatusCode.OK);
        
        //act
        var response = await Client.DeleteAsync($"/api/workouts/{workoutId}");
        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = await Client.GetAsync($"/api/workouts/{workoutId}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var workoutslist = await Client.GetAsync("/api/workouts"); //check that its also not accessible
        workoutslist.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await workoutslist.Content.ReadFromJsonAsync<List<WorkoutCardDto>>();
        result.Should().NotBeNull();
        result!.Select(w=> w.Id).Should().NotContain(workoutId);
    }

    [Fact]
    public async Task DeleteWorkout_Returns404_WhenWorkoutNotowned()
    {
        var userId = await SeedUserAsync("dupe-workout-1@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Pull day");
        var notownderId = await SeedUserAsync("notownder@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(notownderId)}");
        
        //act
        var response = await Client.DeleteAsync($"/api/workouts/{workoutId}");
        //assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}