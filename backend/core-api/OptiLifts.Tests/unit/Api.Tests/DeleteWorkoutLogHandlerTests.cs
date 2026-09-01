using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.DeleteWorkoutLog;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Tests.Api.Tests;

public sealed class DeleteWorkoutLogHandlerTests
{
    private static async Task<OptiLiftsDbContext> CreateMemoryDb()
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
    public async Task Handle_ReturnsFalse_WhenLogDoesNotExist()
    {
        using var db = await CreateMemoryDb();
        var handler = new DeleteWorkoutLogHandler(db, new PlateauDetectionService(new SeriesBuilder(db), db));

        var result = await handler.Handle(
            new DeleteWorkoutLogCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenLogNotOwnedByUser()
    {
        using var db = await CreateMemoryDb();

        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        db.Users.AddRange(
            new User
            {
                Id = ownerId,
                Email = "owner@example.com",
                EmailHash = "owner-hash",
                PasswordHash = "x",
                DisplayName = "Owner"
            },
            new User
            {
                Id = otherUserId,
                Email = "other@example.com",
                EmailHash = "other-hash",
                PasswordHash = "y",
                DisplayName = "Other"
            });
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Upper",
            CreatedBy = ownerId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var entry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = ownerId,
            Scheduled = DateTime.UtcNow,
            Status = ScheduleStatus.Completed
        };
        db.ScheduledEntries.Add(entry);

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-20),
            CompletedAt = DateTime.UtcNow,
            AiModified = false,
            Notes = "Done"
        };
        db.WorkoutLogs.Add(log);
        await db.SaveChangesAsync();

        var handler = new DeleteWorkoutLogHandler(db, new PlateauDetectionService(new SeriesBuilder(db), db));

        var result = await handler.Handle(
            new DeleteWorkoutLogCommand(workout.Id, log.Id, otherUserId),
            CancellationToken.None);

        result.Should().BeFalse();
        (await db.WorkoutLogs.AnyAsync(l => l.Id == log.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_HardDeletesLog_WhenOwnedByUser()
    {
        using var db = await CreateMemoryDb();

        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com",
            EmailHash = "test-hash",
            PasswordHash = "x",
            DisplayName = "Test"
        });
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Lower",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var muscle = new Muscle
        {
            Id = Guid.NewGuid(),
            Name = "Quadriceps"
        };
        db.Muscles.Add(muscle);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Squat",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id
        };
        db.Exercises.Add(exercise);

        var entry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = userId,
            Scheduled = DateTime.UtcNow,
            Status = ScheduleStatus.Completed
        };
        db.ScheduledEntries.Add(entry);

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            CompletedAt = DateTime.UtcNow,
            AiModified = false,
            Notes = "Great session"
        };
        db.WorkoutLogs.Add(log);
        await db.SaveChangesAsync();

        var logSet = new WorkoutSetLog
        {
            Id = Guid.NewGuid(),
            LogId = log.Id,
            ExerciseId = exercise.Id,
            WorkoutExerciseId = null,
            SetId = null,
            Type = SetType.Normal,
            Reps = 8,
            Weight = 100,
            Duration = null,
            Distance = null,
            RestTime = 120,
            GroupNumber = 1,
            Rpe = 8,
            OrderIndex = 0,
            AiSuggested = false,
            LoggedAt = DateTime.UtcNow
        };
        db.WorkoutLogSets.Add(logSet);
        await db.SaveChangesAsync();

        var handler = new DeleteWorkoutLogHandler(db, new PlateauDetectionService(new SeriesBuilder(db), db));

        var result = await handler.Handle(
            new DeleteWorkoutLogCommand(workout.Id, log.Id, userId),
            CancellationToken.None);

        result.Should().BeTrue();
        (await db.WorkoutLogs.AnyAsync(l => l.Id == log.Id)).Should().BeFalse();
        (await db.WorkoutLogSets.AnyAsync(s => s.Id == logSet.Id)).Should().BeFalse();
    }
}
