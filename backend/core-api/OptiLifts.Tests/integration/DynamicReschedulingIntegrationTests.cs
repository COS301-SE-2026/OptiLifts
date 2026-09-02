using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Application.Scheduling.UpdateMissedSessions;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class DynamicReschedulingIntegrationTests : IntegrationTestBase
{
    public DynamicReschedulingIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetandUpdateScheduleConfig_PersistsUserPrefsAcrossRequests()
    {
        var userId = await SeedUserAsync("schedule-user2@example.com");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        //act
        var response = await Client.GetAsync("/api/users/me/schedule/config");
        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var defconfig = await response.Content.ReadFromJsonAsync<UserScheduleConfigDto>();
        defconfig.Should().NotBeNull();
        defconfig!.DynamicSchedulerEnabled.Should().BeFalse();
        defconfig.MaxWorkoutsPerDay.Should().Be(1);

        var newconfig = new UserScheduleConfigDto(
            DynamicSchedulerEnabled: true,
            MaxWorkoutsPerDay: 2,
            MinMuscleRestHours: 36,
            RestDays: new List<string> { "Wednesday", "Sunday" },
            CycleWindowLengthDays: 7,
            CycleStartDate: DateTime.UtcNow.Date
        );

        var resp = await Client.PutAsJsonAsync("/api/users/me/schedule/config", newconfig);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await resp.Content.ReadFromJsonAsync<UserScheduleConfigDto>();
        result.Should().NotBeNull();
        result!.MaxWorkoutsPerDay.Should().Be(2);
        result.MinMuscleRestHours.Should().Be(36);
        result.RestDays.Should().BeEquivalentTo(new[] { "Wednesday", "Sunday" });

        //check persistence
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var createdindb = await db.UserScheduleConfigs.FirstOrDefaultAsync(c => c.UserId == userId);
        createdindb.Should().NotBeNull();
        createdindb!.MaxWorkoutsPerDay.Should().Be(2);
        createdindb.MinMuscleRestHours.Should().Be(36);
    }

    [Fact]
    public async Task UpdateMissedSessions_PendingSessionsAsMissed()
    {
        var userId = await SeedUserAsync("schedule-user3@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Legs");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var pastDate = DateTime.UtcNow.AddDays(-2);
        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            db.ScheduledEntries.Add(new ScheduledEntry
            {
                UserId = userId,
                WorkoutId = workoutId,
                Scheduled = pastDate,
                Status = ScheduleStatus.Scheduled
            });
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsync("/api/users/me/schedule/missed", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UpdateMissedSessionsResult>();
        result.Should().NotBeNull();
        result!.UpdatedCount.Should().Be(1);

        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            var entry = await db.ScheduledEntries.FirstOrDefaultAsync(e => e.UserId == userId);
            entry.Should().NotBeNull();
            entry!.Status.Should().Be(ScheduleStatus.Missed);
        }
    }

    [Fact]
    public async Task TriggerReschedule_ReturnsResult_WhenNoSelectedissedEntries()
    {
        var userId = await SeedUserAsync("schedule-user4@example.com");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        var request = new RescheduleRequestDto(SelectedMissedEntryIds: new List<Guid>());

        var response = await Client.PostAsJsonAsync("/api/users/me/schedule/reschedule", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RescheduleResultDto>();
        result.Should().NotBeNull();
        result!.ExecutionTier.Should().Be("None");
        result.RescheduledEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmReschedule_UpdatesEntryDateAndResetsStatustoScheduled()
    {
        var userId = await SeedUserAsync("schedule-user4@example.com");
        var workoutId = await SeedWorkoutAsync(userId, "Full Body");
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");

        Guid entryId;
        var ogDate = DateTime.UtcNow.AddDays(-1);
        var newDate = DateTime.UtcNow.AddDays(1);
        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            var entry = new ScheduledEntry
            {
                UserId = userId,
                WorkoutId = workoutId,
                Scheduled = ogDate,
                Status = ScheduleStatus.Missed
            };
            db.ScheduledEntries.Add(entry);
            await db.SaveChangesAsync();
            entryId = entry.Id;
        }

        var confirmItems = new List<ConfirmRescheduleItemDto>
        {
            new ConfirmRescheduleItemDto(EntryId: entryId, NewScheduledAt: newDate)
        };

        var response = await Client.PostAsJsonAsync("/api/users/me/schedule/reschedule/confirm", confirmItems);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var scope = Fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            var entry = await db.ScheduledEntries.FindAsync(entryId);
            entry.Should().NotBeNull();
            entry!.Status.Should().Be(ScheduleStatus.Scheduled);
            entry.Scheduled.Should().BeCloseTo(newDate, TimeSpan.FromSeconds(1));
        }
    }
}