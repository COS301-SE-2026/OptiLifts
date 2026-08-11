using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class DeleteAccountIntegrationTests : IntegrationTestBase
{
    public DeleteAccountIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task DeleteAccount_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        var res = await Client.DeleteAsync("/api/users/me");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_Authenticated_DeletesUserAndData()
    {
        var userId = await SeedUserAsync("jordan@gmail.com");
        var workoutId = await SeedWorkoutAsync(userId, "Legs");

        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
        var response = await Client.DeleteAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains("access_token=;"));

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var delUser = await db.Users.FindAsync(userId);
        var delWorkout = await db.Workouts.FindAsync(workoutId);

        delUser.Should().BeNull();
        delWorkout.Should().BeNull();
    }
}