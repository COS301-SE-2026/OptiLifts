using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Exercises.GetExercises;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class ExerciseComponentIntegrationTests : IntegrationTestBase
{
    public ExerciseComponentIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateCustomExercise_CanBeRetrievedByGetExercises()
    {
        var user = await SeedUserAsync("integration-exercise-1@optilifts.com");
        await SeedMuscleAsync("Biceps");
        var client = CreateAuthenticatedClient(user);

        using var createContent = BuildCustomExerciseContent(
            name: "Custom Curl",
            mechanic: "isolation",
            equipment: "dumbbell",
            category: "Strength",
            primaryMuscles: ["Biceps"],
            secondaryMuscles: []);

        var createResponse = await client.PostAsync("/api/exercises/custom", createContent);

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateResult>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        var getResponse = await client.GetAsync("/api/exercises");
        getResponse.EnsureSuccessStatusCode();

        var exercises = await getResponse.Content.ReadFromJsonAsync<ExerciseDto[]>();
        exercises.Should().NotBeNull();
        exercises.Should().Contain(e => e.Name == "Custom Curl" && e.IsCustom && e.PrimaryMuscles.Contains("Biceps"));
    }

    [Fact]
    public async Task GetExercises_ReturnsOnlyAuthenticatedUsersExercises()
    {
        var userOne = await SeedUserAsync("integration-exercise-2@optilifts.com");
        var userTwo = await SeedUserAsync("integration-exercise-3@optilifts.com");

        await SeedMuscleAsync("Lats");
        await SeedMuscleAsync("Chest");

        var clientOne = CreateAuthenticatedClient(userOne);
        var clientTwo = CreateAuthenticatedClient(userTwo);

        using var userOneContent = BuildCustomExerciseContent(
            name: "UserOne Exercise",
            mechanic: "compound",
            equipment: "barbell",
            category: "Strength",
            primaryMuscles: ["Lats"],
            secondaryMuscles: []);

        var resp1 = await clientOne.PostAsync("/api/exercises/custom", userOneContent);
        resp1.EnsureSuccessStatusCode();

        using var userTwoContent = BuildCustomExerciseContent(
            name: "UserTwo Exercise",
            mechanic: "compound",
            equipment: "barbell",
            category: "Strength",
            primaryMuscles: ["Chest"],
            secondaryMuscles: []);

        var resp2 = await clientTwo.PostAsync("/api/exercises/custom", userTwoContent);
        resp2.EnsureSuccessStatusCode();

        var getOne = await clientOne.GetAsync("/api/exercises");
        getOne.EnsureSuccessStatusCode();
        var exercisesOne = await getOne.Content.ReadFromJsonAsync<ExerciseDto[]>();

        exercisesOne.Should().NotBeNull();
        exercisesOne!.Select(e => e.Name).Should().Contain("UserOne Exercise");
        exercisesOne.Select(e => e.Name).Should().NotContain("UserTwo Exercise");
    }

    private record CreateResult(Guid Id);

    private static MultipartFormDataContent BuildCustomExerciseContent(
        string name,
        string mechanic,
        string equipment,
        string category,
        IEnumerable<string> primaryMuscles,
        IEnumerable<string> secondaryMuscles)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(name, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Name) },
            { new StringContent(mechanic, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Mechanic) },
            { new StringContent(equipment, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Equipment) },
            { new StringContent(category, Encoding.UTF8), nameof(CreateCustomExerciseRequest.Category) },
        };

        foreach (var muscle in primaryMuscles)
        {
            content.Add(new StringContent(muscle, Encoding.UTF8), nameof(CreateCustomExerciseRequest.PrimaryMuscles));
        }

        foreach (var muscle in secondaryMuscles)
        {
            content.Add(new StringContent(muscle, Encoding.UTF8), nameof(CreateCustomExerciseRequest.SecondaryMuscles));
        }

        return content;
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = Fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
        return client;
    }

    private async Task<Domain.Workouts.Muscle> SeedMuscleAsync(string name)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var existing = await db.Muscles.FirstOrDefaultAsync(m => m.Name == name);
        if (existing is not null) return existing;

        var muscle = new Domain.Workouts.Muscle { Name = name };
        db.Muscles.Add(muscle);
        await db.SaveChangesAsync();
        return muscle;
    }
}