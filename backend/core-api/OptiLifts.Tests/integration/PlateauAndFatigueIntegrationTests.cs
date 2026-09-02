using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Domain.Training;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public class PlateauAndFatigueIntegrationTests : IntegrationTestBase
{
    public PlateauAndFatigueIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private async Task SeedingTrendsAsync(Guid userId, Guid exerciseId)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        
        db.ExerciseTrends.Add(new ExerciseTrend
        {
            UserId = userId,
            ExerciseId = exerciseId,
            Status = TrendStatus.Plateau,
            WindowEnd = DateTime.UtcNow,
            WindowStart = DateTime.UtcNow.AddDays(-77),
            SessionsUsed = 12
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPlateauPage_ReturnsPlateauingExer()
    {
        var userId = await SeedUserAsync("plateau-int@example.com");
        var exerId = await SeedExerciseAsync("Integration Bench");
        
        await SeedingTrendsAsync(userId, exerId);
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var resp = await Client.GetAsync("/api/training/plateau-page");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var bodyContent = await resp.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        bodyContent.Should().ContainSingle(e => e["exerciseName"]!.ToString() == "Integration Bench");
    }

    [Fact]
    public async Task GetPlateauPage_ReturnsEmptyForNoTrendsExist()
    {
        var userId = await SeedUserAsync("plateau-empty@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var resp = await Client.GetAsync("/api/training/plateau-page");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var bodyContent = await resp.Content.ReadFromJsonAsync<List<object>>();
        bodyContent.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordAcuteFatigue_ReturnsOkPersistsEvent()
    {
        var userId = await SeedUserAsync("fatigue-int@example.com");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var resp = await Client.PostAsJsonAsync("/api/training/acute-fatigue", new { MuscleGroup = "Chest" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var evt = await db.TrainingEvents.FirstOrDefaultAsync(e => e.UserId == userId && e.Type == TrainingEventType.AcuteFatigueFlagged);
        evt.Should().NotBeNull();
        evt!.Scope.Should().Be("Chest");
    }

    [Fact]
    public async Task ReplaceWorkoutExerReturnsNoContentUpdatesExer()
    {
        var userId = await SeedUserAsync("swap-int@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Push Day");
        var oldExerId = await SeedExerciseAsync("Old Bench");
        var newExerId = await SeedExerciseAsync("New Bench");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            db.WorkoutExercises.Add(new WorkoutExercise { WorkoutId = workoutId, ExerciseId = oldExerId, OrderIndex = 0 });
            await db.SaveChangesAsync();
        }

        var resp = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/exercises/{oldExerId}", new { newExerId });
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var verifyScope = Fixture.Factory.Services.CreateAsyncScope();
        var verifyDatabase = verifyScope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var updated = await verifyDatabase.WorkoutExercises.AsNoTracking().FirstAsync(we => we.WorkoutId == workoutId);
        updated.ExerciseId.Should().Be(newExerId);
    }

        [Fact]
    public async Task ReplaceWorkoutExerReturnsNotFoundForWorkoutBelongsToAnotherUser()
    {
        var ownerId = await SeedUserAsync("swap-owner@example.com");
        var attackerId = await SeedUserAsync("swap-attacker@example.com");
        var workoutId = await SeedWorkoutAsync(ownerId, "Owner's Push Day");
        var oldExerId = await SeedExerciseAsync("Owner Bench");
        var newExerId = await SeedExerciseAsync("Owner New Bench");

        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            db.WorkoutExercises.Add(new WorkoutExercise { WorkoutId = workoutId, ExerciseId = oldExerId, OrderIndex = 0 });
            await db.SaveChangesAsync();
        }

        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(attackerId)}");
        var resp = await Client.PutAsJsonAsync($"/api/workouts/{workoutId}/exercises/{oldExerId}", new { NewExerciseId = newExerId });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
