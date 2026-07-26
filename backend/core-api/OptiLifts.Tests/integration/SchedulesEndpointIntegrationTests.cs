using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Application.Scheduling.UpdateScheduledSessionStatus;
using OptiLifts.Domain.Workouts;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class SchedulesEndpointIntegrationTests : IntegrationTestBase
{
    public SchedulesEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateScheduledSession_Returns201_WhenRequestValid()
    {
        var userId = await SeedUserAsync("schedule-user1@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Morning Routine");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
        var scheduledTime = DateTime.UtcNow.AddDays(1);
        var request = new SchedulesController.CreateScheduledSessionRequest(
            WorkoutId: workoutId,
            ScheduledAt: scheduledTime,
            Status: ScheduleStatus.Scheduled
        );
        //act
        var response = await Client.PostAsJsonAsync("/api/users/me/schedule/sessions", request);
        //assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateScheduledSessionResult>();
        result.Should().NotBeNull();
        result!.WorkoutId.Should().Be(workoutId);
        result.ScheduledAt.Should().BeCloseTo(scheduledTime, TimeSpan.FromSeconds(1));
        result.Status.Should().Be(ScheduleStatus.Scheduled);
    }

    [Fact]
    public async Task GetSchedule_ReturnsUserScheduledSessions()
    {
        var userId = await SeedUserAsync("schedule-user2@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Evening workout");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
        var scheduledTime = DateTime.UtcNow.AddDays(2);
        var request = new SchedulesController.CreateScheduledSessionRequest(
            WorkoutId: workoutId,
            ScheduledAt: scheduledTime,
            Status: ScheduleStatus.Scheduled
        );
        var creatresp = await Client.PostAsJsonAsync("/api/users/me/schedule/sessions", request);
        creatresp.EnsureSuccessStatusCode();

        var startDate = scheduledTime.Date.ToString("yyyy-MM-dd");
        var endDate = scheduledTime.Date.ToString("yyyy-MM-dd");
        var response = await Client.GetAsync($"/api/users/me/schedule?startDate={startDate}&endDate={endDate}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ScheduledEntryDto[]>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].WorkoutId.Should().Be(workoutId);
        result[0].WorkoutName.Should().Be("Evening workout");
    }

    [Fact]
    public async Task UpdateScheduledSessionStatus_UpdatesStatus_WhenSessionExists()
    {
        var userId = await SeedUserAsync("schedule-user3@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Cardio time");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var scheduledTime = DateTime.UtcNow.AddDays(3);
        var request = new SchedulesController.CreateScheduledSessionRequest(
            WorkoutId: workoutId,
            ScheduledAt: scheduledTime,
            Status: ScheduleStatus.Scheduled
        );
        var creatresp = await Client.PostAsJsonAsync("/api/users/me/schedule/sessions", request);
        var createdresult = await creatresp.Content.ReadFromJsonAsync<CreateScheduledSessionResult>();
        createdresult.Should().NotBeNull();

        var patchrequest = new SchedulesController.UpdateScheduledSessionStatusRequest(ScheduleStatus.Completed);

        var response = await Client.PatchAsJsonAsync($"/api/users/me/schedule/sessions/{createdresult!.Id}", patchrequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UpdateScheduledSessionStatusResult>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdresult.Id);
        result.Status.Should().Be(ScheduleStatus.Completed);
    }

    [Fact]
    public async Task DeleteScheduledSession_RemoveSession_WHenSessionExists()
    {
        var userId = await SeedUserAsync("schedule-user4@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Legs");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var scheduledTime = DateTime.UtcNow.AddDays(4);
        var request = new SchedulesController.CreateScheduledSessionRequest(
            WorkoutId: workoutId,
            ScheduledAt: scheduledTime,
            Status: ScheduleStatus.Scheduled
        );
        var creatresp = await Client.PostAsJsonAsync("/api/users/me/schedule/sessions", request);
        var createdresult = await creatresp.Content.ReadFromJsonAsync<CreateScheduledSessionResult>();
        createdresult.Should().NotBeNull();

        var response = await Client.DeleteAsync($"/api/users/me/schedule/sessions/{createdresult!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var startDate = scheduledTime.Date.ToString("yyyy-MM-dd");
        var endDate = scheduledTime.Date.ToString("yyyy-MM-dd");
        var result = await Client.GetAsync($"/api/users/me/schedule?startDate={startDate}&endDate={endDate}");
        var schedule = await result.Content.ReadFromJsonAsync<ScheduledEntryDto[]>();
        schedule.Should().BeEmpty();
    }
}