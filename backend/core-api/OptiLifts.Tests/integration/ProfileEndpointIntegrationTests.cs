using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Profile;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Domain.Workouts;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class ProfileEndpointIntegrationTests : IntegrationTestBase
{
    public ProfileEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetOverview_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");

        var response = await Client.GetAsync("/api/profile/overview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_WithCompletedSession_ReturnsProfileOverview()
    {
        await SeedCompletedProfileSessionAsync("profile-overview@optilifts.com");

        var response = await Client.GetAsync("/api/profile/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProfileOverviewDto>();

        result.Should().NotBeNull();
        result!.Profile.Name.Should().Be("Test User");
        result.Profile.Email.Should().Be("profile-overview@optilifts.com");
        result.RecentWorkouts.Should().NotBeEmpty();
        result.ChartData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCalendar_InvalidMonth_ReturnsBadRequest()
    {
        var userId = await SeedUserAsync("profile-calendar-invalid@optilifts.com");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var response = await Client.GetAsync("/api/profile/calendar?year=2026&month=13");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCalendar_WithCompletedSession_ReturnsCalendarEntries()
    {
        var seeded = await SeedCompletedProfileSessionAsync("profile-calendar@optilifts.com");

        var response = await Client.GetAsync($"/api/profile/calendar?year={seeded.CompletedAt.Year}&month={seeded.CompletedAt.Month}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProfileCalendarDto>();

        result.Should().NotBeNull();
        result!.Entries.Should().Contain(entry =>
            entry.WorkoutId == seeded.WorkoutId &&
            entry.LogId == seeded.LogId &&
            entry.Date == seeded.CompletedAt.ToString("yyyy-MM-dd"));
    }

    private async Task<(Guid WorkoutId, Guid LogId, DateTime CompletedAt)> SeedCompletedProfileSessionAsync(string email)
    {
        var userId = await SeedUserAsync(email);
        var exerciseId = await SeedExerciseAsync("Bench Press");

        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var createWorkoutBody = new CreateWorkoutRequest(
            FolderId: null,
            Name: "Profile Push Day",
            Exercises:
            [
                new CreateWorkoutExerciseRequest(
                    ExerciseId: exerciseId,
                    OrderIndex: 1,
                    GroupKey: null,
                    Sets:
                    [
                        new CreateWorkoutSetRequest("Normal", 8, 80f, null, null, 1, 90)
                    ])
            ],
            Groups: []);

        var createWorkoutResponse = await Client.PostAsJsonAsync("/api/workouts", createWorkoutBody);
        createWorkoutResponse.EnsureSuccessStatusCode();
        var createdWorkout = await createWorkoutResponse.Content.ReadFromJsonAsync<CreateWorkoutResult>();
        createdWorkout.Should().NotBeNull();

        var completedAt = DateTime.UtcNow.AddDays(-2);
        var createScheduledSessionRequest = new SchedulesController.CreateScheduledSessionRequest(
            WorkoutId: createdWorkout!.WorkoutId,
            ScheduledAt: completedAt,
            Status: ScheduleStatus.Completed);

        var createScheduledSessionResponse = await Client.PostAsJsonAsync("/api/users/me/schedule/sessions", createScheduledSessionRequest);
        createScheduledSessionResponse.EnsureSuccessStatusCode();
        var createdSession = await createScheduledSessionResponse.Content.ReadFromJsonAsync<CreateScheduledSessionResult>();
        createdSession.Should().NotBeNull();

        var logId = Guid.NewGuid();
        var createLogRequest = new CreateWorkoutLogReq(
            LogId: logId,
            EntryId: createdSession!.Id,
            Notes: "Strong session",
            StartedAt: completedAt.AddMinutes(-45),
            CompletedAt: completedAt,
            Exercises:
            [
                new CreateWorkoutLogExerciseReq(
                    ExerciseId: exerciseId,
                    WorkoutExerciseId: null,
                    OrderIndex: 1,
                    GroupNumber: 1,
                    Sets:
                    [
                        new CreateWorkoutLogSetReq(
                            SetId: null,
                            Type: "Normal",
                            Reps: 8,
                            Weight: 82.5f,
                            Duration: null,
                            Distance: null,
                            RestTime: 90,
                            Rpe: 8f,
                            GroupNumber: 1,
                            OrderIndex: 1)
                    ])
            ]);

        var createLogResponse = await Client.PostAsJsonAsync($"/api/workouts/{createdWorkout.WorkoutId}/logs", createLogRequest);
        createLogResponse.EnsureSuccessStatusCode();

        return (createdWorkout.WorkoutId, logId, completedAt);
    }
}