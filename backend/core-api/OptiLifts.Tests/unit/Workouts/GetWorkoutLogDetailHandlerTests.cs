using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutLogDetail;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Unit.Workouts;

public class GetWorkoutLogDetailHandlerTests
{
    private static OptiLiftsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new OptiLiftsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Handle_ReturnsWorkoutLogDetail_WithDurationAndLoggedSets()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "log@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Log User"
        };

        var muscle = new Muscle { Id = Guid.NewGuid(), Name = "Quadriceps" };
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Full Body",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc)
        };

        var running = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Running",
            Mechanic = "compound",
            Equipment = "bodyweight",
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.DistanceDuration
        };

        context.Users.Add(user);
        context.Muscles.Add(muscle);
        context.Workouts.Add(workout);
        context.Exercises.Add(running);
        await context.SaveChangesAsync();

        var workoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = running.Id,
            OrderIndex = 0
        };

        context.WorkoutExercises.Add(workoutExercise);
        await context.SaveChangesAsync();

        var entryId = Guid.NewGuid();
        var startedAt = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc);
        var completedAt = new DateTime(2026, 7, 2, 9, 5, 0, DateTimeKind.Utc);

        context.ScheduledEntries.Add(new ScheduledEntry
        {
            Id = entryId,
            UserId = user.Id,
            WorkoutId = workout.Id,
            Scheduled = startedAt,
            Status = ScheduleStatus.Completed
        });
        await context.SaveChangesAsync();

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            AiModified = false,
            Notes = "test"
        };

        context.WorkoutLogs.Add(log);
        await context.SaveChangesAsync();

        context.WorkoutLogSets.Add(new WorkoutSetLog
        {
            Id = Guid.NewGuid(),
            LogId = log.Id,
            ExerciseId = running.Id,
            SetId = null,
            Type = SetType.Normal,
            Reps = 1800,
            Weight = 0,
            Rpe = 7.5f,
            OrderIndex = 0,
            AiSuggested = false,
            LoggedAt = new DateTime(2026, 7, 2, 8, 5, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var handler = new GetWorkoutLogDetailHandler(context);
        var result = await handler.Handle(new GetWorkoutLogDetailQuery(workout.Id, log.Id, user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.WorkoutId.Should().Be(workout.Id);
        result.LogId.Should().Be(log.Id);
        result.Name.Should().Be("Full Body");
        result.CompletedAt.Should().Be(completedAt);
        result.Duration.Should().Be("01:05");
        result.PrimaryMuscleGroups.Should().ContainSingle().Which.Should().Be("Quadriceps");
        result.ExercisePreview.Should().ContainSingle().Which.Should().Be("Running");
        result.Exercises.Should().HaveCount(1);
        result.Exercises[0].ExerciseType.Should().Be("distance-duration");
        result.Exercises[0].Sets.Should().HaveCount(1);
        result.Exercises[0].Sets[0].Reps.Should().Be(1800);
        result.Exercises[0].Sets[0].Weight.Should().Be(0);
        result.Exercises[0].Sets[0].Rpe.Should().Be(7.5f);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenLogDoesNotMatchWorkoutOrUser()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Owner"
        };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Workout",
            CreatedBy = user.Id
        };

        var entryId = Guid.NewGuid();
        var startedAt = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc);
        var completedAt = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc);

        context.Users.Add(user);
        context.Workouts.Add(workout);
        context.ScheduledEntries.Add(new ScheduledEntry
        {
            Id = entryId,
            UserId = user.Id,
            WorkoutId = workout.Id,
            Scheduled = startedAt,
            Status = ScheduleStatus.Completed
        });
        await context.SaveChangesAsync();

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            AiModified = false
        };

        context.WorkoutLogs.Add(log);
        await context.SaveChangesAsync();

        var handler = new GetWorkoutLogDetailHandler(context);
        var result = await handler.Handle(new GetWorkoutLogDetailQuery(workout.Id, log.Id, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}