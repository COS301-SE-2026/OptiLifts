using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class GetWorkoutsEndpointIntegrationTests : IntegrationTestBase
{
    public GetWorkoutsEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetWorkouts_ReturnsSeededWorkoutsForAuthenticatedUser()
    {
        var user = await SeedUserAsync("integration-user-1@optilifts.com");
        await SeedWorkoutAsync(
            user,
            new DateTime(2026, 05, 19, 10, 0, 0, DateTimeKind.Utc),
            "Push Day A",
            ("Bench Press", new[] { "Chest" }),
            ("Overhead Press", new[] { "Shoulders" }));

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(user)}");
        var response = await Client.GetAsync("/api/workouts");
        response.EnsureSuccessStatusCode();

        var workouts = await response.Content.ReadFromJsonAsync<WorkoutCardDto[]>();
        workouts.Should().NotBeNull();
        workouts.Should().HaveCount(1);

        var workout = workouts![0];
        workout.Name.Should().Be("Push Day A");
        workout.ExerciseCount.Should().Be(2);
        workout.ExercisePreview.Should().Equal("Bench Press", "Overhead Press");
        workout.PrimaryMuscleGroups.Should().Equal("Chest", "Shoulders");
        workout.CreatedAt.Should().BeCloseTo(new DateTime(2026, 05, 19, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetWorkouts_ReturnsOnlyAuthenticatedUsersWorkoutsOrderedNewestFirst()
    {
        var userOne = await SeedUserAsync("integration-user-2@optilifts.com");
        var otherUser = await SeedUserAsync("integration-user-3@optilifts.com");

        await SeedWorkoutAsync(
            userOne,
            new DateTime(2026, 05, 19, 9, 0, 0, DateTimeKind.Utc),
            "Old Workout",
            ("Row", new[] { "Back" }));
        await SeedWorkoutAsync(
            userOne,
            new DateTime(2026, 05, 19, 11, 0, 0, DateTimeKind.Utc),
            "New Workout",
            ("Squat", new[] { "Quadriceps", "Glutes" }));
        await SeedWorkoutAsync(
            otherUser,
            new DateTime(2026, 05, 19, 12, 0, 0, DateTimeKind.Utc),
            "Other User Workout",
            ("Bench Press", new[] { "Chest" }));

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userOne)}");
        var response = await Client.GetAsync("/api/workouts");

        response.EnsureSuccessStatusCode();

        var workouts = await response.Content.ReadFromJsonAsync<WorkoutCardDto[]>();
        workouts.Should().NotBeNull();
        workouts.Should().HaveCount(2);
        workouts![0].Name.Should().Be("New Workout");
        workouts[1].Name.Should().Be("Old Workout");
        workouts.Select(workout => workout.Name).Should().NotContain("Other User Workout");
    }

    private async Task SeedWorkoutAsync(
        Guid userId,
        DateTime createdAt,
        string workoutName,
        params (string ExerciseName, string[] PrimaryMuscles)[] exercises)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
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

            var primaryMuscleName = primaryMuscles[0];
            var primaryMuscle = await db.Muscles.FirstOrDefaultAsync(m => m.Name == primaryMuscleName);
            if (primaryMuscle is null)
            {
                primaryMuscle = new Muscle
                {
                    Name = primaryMuscleName
                };
                db.Muscles.Add(primaryMuscle);
                await db.SaveChangesAsync();
            }
            var exercise = new Exercise
            {
                Name = exerciseName,
                Mechanic = "compound",
                Equipment = "barbell",
                ExerciseType = ExerciseType.WeightReps,
                PrimaryMuscleId = primaryMuscle.Id,
                UserId = null,
                ImageUrl = null
            };
            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            var workoutExercise = new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = exercise.Id,
                OrderIndex = index
            };
            db.WorkoutExercises.Add(workoutExercise);
            await db.SaveChangesAsync();

            db.Sets.Add(new WorkoutSet
            {
                WorkoutExerciseId = workoutExercise.Id,
                Type = SetType.Normal,
                Reps = 8,
                Weight = 100,
                OrderIndex = 1,
                RestTime = 90
            });
        }
        await db.SaveChangesAsync();
    }
}
