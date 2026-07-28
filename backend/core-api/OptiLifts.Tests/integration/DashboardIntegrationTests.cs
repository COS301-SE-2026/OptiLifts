using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Profile;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Application.Scheduling.GetScheduleAnalytics;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Domain.Workouts;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class DashboardIntegrationTests : IntegrationTestBase
{
    public DashboardIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task DashboardDataFlow_ReturnsProfileUpcomingCompletedAndAnalytics()
    {
        var seeded = await SeedDashboardDataAsync();

        var today = DateTime.UtcNow.Date;
        var upcomingEnd = today.AddDays(30);
        var completedStart = today.AddYears(-1);
        var completedEnd = today;

        var profileResponse = await Client.GetAsync("/api/profile/overview");
        var upcomingResponse = await Client.GetAsync($"/api/users/me/schedule?startDate={today:yyyy-MM-dd}&endDate={upcomingEnd:yyyy-MM-dd}");
        var completedResponse = await Client.GetAsync($"/api/users/me/schedule?startDate={completedStart:yyyy-MM-dd}&endDate={completedEnd:yyyy-MM-dd}&status=Completed");
        var analyticsResponse = await Client.GetAsync($"/api/users/me/schedule/analytics?startDate={completedStart:yyyy-MM-dd}&endDate={completedEnd:yyyy-MM-dd}&status=Completed");

        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        upcomingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        completedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        analyticsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileOverviewDto>();
        var upcoming = await upcomingResponse.Content.ReadFromJsonAsync<ScheduledEntryDto[]>();
        var completed = await completedResponse.Content.ReadFromJsonAsync<ScheduledEntryDto[]>();
        var analytics = await analyticsResponse.Content.ReadFromJsonAsync<ScheduleAnalyticsDto>();

        profile.Should().NotBeNull();
        profile!.Profile.Email.Should().Be("dashboard-user@optilifts.com");
        profile.RecentWorkouts.Should().NotBeEmpty();

        upcoming.Should().NotBeNull();
        upcoming!.Should().Contain(entry => entry.Id == seeded.UpcomingEntryId && entry.Status == "Scheduled");

        completed.Should().NotBeNull();
        completed!.Should().Contain(entry => entry.Id == seeded.CompletedEntryId && entry.Status == "Completed");

        analytics.Should().NotBeNull();
        analytics!.TotalWorkouts.Should().Be(1);
        analytics.TotalSets.Should().Be(2);
        analytics.TotalVolume.Should().Be(980f);
    }

    [Fact]
    public async Task DashboardDataFlow_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");

        var profileResponse = await Client.GetAsync("/api/profile/overview");
        var scheduleResponse = await Client.GetAsync("/api/users/me/schedule");
        var analyticsResponse = await Client.GetAsync("/api/users/me/schedule/analytics");

        profileResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        analyticsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(Guid CompletedEntryId, Guid UpcomingEntryId)> SeedDashboardDataAsync()
    {
        var userId = await SeedUserAsync("dashboard-user@optilifts.com");
        var exerciseId = await SeedExerciseAsync("Incline Bench Press");

        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWorkoutBody = new CreateWorkoutRequest(
            FolderId: null,
            Name: "Dashboard Push Day",
            Exercises:
            [
                new CreateWorkoutExerciseRequest(
                    ExerciseId: exerciseId,
                    OrderIndex: 1,
                    GroupKey: null,
                    Sets:
                    [
                        new CreateWorkoutSetRequest("Normal", 10, 50f, null, null, 1, 90),
                        new CreateWorkoutSetRequest("Normal", 8, 60f, null, null, 2, 120)
                    ])
            ],
            Groups: []);

        var createWorkoutResponse = await Client.PostAsJsonAsync("/api/workouts", createWorkoutBody);
        createWorkoutResponse.EnsureSuccessStatusCode();
        var createdWorkout = await createWorkoutResponse.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        createdWorkout.Should().NotBeNull();

        var completedScheduledAt = DateTime.UtcNow.AddDays(-1);
        var completedSessionRequest = new SchedulesController.CreateScheduledSessionRequest(
            WorkoutId: createdWorkout!.WorkoutId,
            ScheduledAt: completedScheduledAt,
            Status: ScheduleStatus.Completed);
        var completedSessionResponse = await Client.PostAsJsonAsync("/api/users/me/schedule/sessions", completedSessionRequest);
        completedSessionResponse.EnsureSuccessStatusCode();
        var completedSession = await completedSessionResponse.Content.ReadFromJsonAsync<CreateScheduledSessionResult>();
        completedSession.Should().NotBeNull();

        var upcomingSessionRequest = new SchedulesController.CreateScheduledSessionRequest(
            WorkoutId: createdWorkout.WorkoutId,
            ScheduledAt: DateTime.UtcNow.AddDays(2),
            Status: ScheduleStatus.Scheduled);
        var upcomingSessionResponse = await Client.PostAsJsonAsync("/api/users/me/schedule/sessions", upcomingSessionRequest);
        upcomingSessionResponse.EnsureSuccessStatusCode();
        var upcomingSession = await upcomingSessionResponse.Content.ReadFromJsonAsync<CreateScheduledSessionResult>();
        upcomingSession.Should().NotBeNull();

        var logId = Guid.NewGuid();
        var createLogRequest = new CreateWorkoutLogReq(
            LogId: logId,
            EntryId: completedSession!.Id,
            Notes: "Dashboard completed workout",
            StartedAt: completedScheduledAt.AddMinutes(-50),
            CompletedAt: completedScheduledAt,
            Exercises:
            [
                new CreateWorkoutLogExerciseReq(
                    ExerciseId: exerciseId,
                    WorkoutExerciseId: null,
                    OrderIndex: 1,
                    GroupNumber: 1,
                    Sets:
                    [
                        new CreateWorkoutLogSetReq(null, "Normal", 10, 50f, null, null, 90, 7.5f, 1, 1),
                        new CreateWorkoutLogSetReq(null, "Normal", 8, 60f, null, null, 120, 8f, 1, 2)
                    ])
            ]);

        var createLogResponse = await Client.PostAsJsonAsync($"/api/workouts/{createdWorkout.WorkoutId}/logs", createLogRequest);
        createLogResponse.EnsureSuccessStatusCode();

        return (completedSession.Id, upcomingSession!.Id);
    }
}