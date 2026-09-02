using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Infrastructure.Scheduling.Reschedule;
using Moq;
using Moq.Protected;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql.Internal;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;

namespace OptiLifts.Tests.Api.Tests.Schedule;

public sealed class TriggerRescheduleHandlerTests
{
    private static async Task<OptiLiftsDbContext> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new OptiLiftsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task Handle_ReturnsEmptyResult_WhenNOEntriesExist()
    {
        using var db = await CreateDbContextAsync();
        var mockFactory = new Mock<IHttpClientFactory>();
        var handler = new TriggerRescheduleHandler(db, mockFactory.Object);
        var command = new TriggerRescheduleCommand(Guid.NewGuid(), new List<Guid>());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ExecutionTier.Should().Be("None");
        result.ExecutionTimeMs.Should().Be(0);
        result.RescheduledEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SuccessfullyCallsApiAndReturnsrescheduleResult()
    {
        using var db = await CreateDbContextAsync();
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            EmailHash = "testhash",
            PasswordHash = "passwprd",
            DisplayName = "Test User 1"
        };
        db.Users.Add(user);

        db.UserScheduleConfigs.Add(new UserScheduleConfig
        {
            UserId = userId,
            MaxWorkoutsPerDay = 2,
            MinMuscleRestHours  = 24,
            RestDays = new List<string> {"Sunday"},
            CycleWindowLengthDays = 7,
            CycleStartDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)
        });

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Upper Body",
            CreatedBy = userId
        };
        var muscle = new Muscle
        {
            Id = Guid.NewGuid(),
            Name = "Chest"
        };
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            PrimaryMuscleId = muscle.Id
        };
        var workoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            OrderIndex = 1
        };

        db.Workouts.Add(workout);
        db.Muscles.Add(muscle);
        db.Exercises.Add(exercise);
        db.WorkoutExercises.Add(workoutExercise);

        var missedEntry = Guid.NewGuid();
        db.ScheduledEntries.Add(new ScheduledEntry
        {
            Id = missedEntry,
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddHours(9), DateTimeKind.Utc),
            Status = ScheduleStatus.Missed
        });
        await db.SaveChangesAsync();

        //mock the httpclient respnse
        var airesponsePayload = new
        {
            user_id = userId.ToString(),
            execution_tier = "Tier1_FastPath",
            execution_time_ms = 12,
            rescheduled_entries = new[]{
            new
            {
                entry_id = missedEntry.ToString(),
                workout_id = workout.Id.ToString(),
                workout_name = "Upper Body",
                original_scheduled_at = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddHours(9), DateTimeKind.Utc),
                new_scheduled_at = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                action = "Rescheduled"

            }
        },
            dropped_entries = Array.Empty<object>()
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(airesponsePayload)
            });
        var httpclient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8000")
        };
        
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("AiApi")).Returns(httpclient);
        var handler = new TriggerRescheduleHandler(db, mockFactory.Object);
        var command = new TriggerRescheduleCommand(userId, new List<Guid>{missedEntry});
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ExecutionTier.Should().Be("Tier1_FastPath");
        result.RescheduledEntries.Should().HaveCount(1);
        result.RescheduledEntries[0].EntryId.Should().Be(missedEntry);
        result.RescheduledEntries[0].Action.Should().Be("Rescheduled");
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperationException_WhenAiApiResponseIsNull()
    {
        using var db = await CreateDbContextAsync();
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            EmailHash = "testhash",
            PasswordHash = "passwprd",
            DisplayName = "Test User 1"
        };
        db.Users.Add(user);
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Upper Body",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);

        var entryid = Guid.NewGuid();
        db.ScheduledEntries.Add(new ScheduledEntry
        {
            Id = entryid,
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddHours(10), DateTimeKind.Utc),
            Status = ScheduleStatus.Scheduled
        });
        await db.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null")
            });
        var httpclient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8000")
        };
        
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient("AiApi")).Returns(httpclient);
        var handler = new TriggerRescheduleHandler(db, mockFactory.Object);
        var command = new TriggerRescheduleCommand(userId, new List<Guid>{entryid});
        var result = async () => await handler.Handle(command, CancellationToken.None);
        await result.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid response from Python ai");

    }
}