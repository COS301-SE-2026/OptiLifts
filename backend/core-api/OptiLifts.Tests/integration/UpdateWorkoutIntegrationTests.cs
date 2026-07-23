using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Application.Workouts.UpdateWorkout;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class UpdateWorkoutIntegrationTests : IntegrationTestBase
{
    public UpdateWorkoutIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PutWorkout_Returns200ok_ANdUpdatesWorkoutDetails()
    {
        var userId = await SeedUserAsync("edit-workout1@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Original name");
        var exerciseId = await SeedExerciseAsync("Push Up");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        Guid folderId;
        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            var folder = await db.Folders.FirstAsync(f => f.UserId == userId);
            folderId = folder.Id;
        }
        
        var updateRequest = new UpdateWorkoutRequest(
            FolderId: folderId,
            Name: "Updated name",
            Exercises: [
                new UpdateWorkoutExerciseDto(
                    ExerciseId: exerciseId,
                    OrderIndex: 0, //is this changed?
                    Sets: [
                        new UpdateWorkoutSetDto(
                            Type: "Normal",
                            Reps: 12,
                            Weight: null,
                            Duration: null,
                            Distance: null,
                            OrderIndex: 0, //is this changed?
                            RestTime: 60
                        )
                    ]
                )
            ]
        );
        //act
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}", updateRequest);
        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getresponse = await Client.GetAsync($"/api/workouts/{workoutId}");
        getresponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await getresponse.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated name");
        result.Exercises.Should().HaveCount(1);
        result.Exercises[0].Name.Should().Be("Push Up");
        result.Exercises[0].Sets.Should().HaveCount(1);
        result.Exercises[0].Sets[0].Reps.Should().Be(12);
    }

    [Fact]
    public async Task PutWorkout_REturns404NotFount_whenWorkoutNotOwned()
    {
        var userId = await SeedUserAsync("edit-workout1@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Original name");
        var notownderId = await SeedUserAsync("notownder@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(notownderId)}");

        var request = new UpdateWorkoutRequest(
            FolderId: null,
            Name: "My Workout",
            Exercises: []
        );
        var response = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}