using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.UpdateWorkoutLog;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Tests.Api.Tests;

public sealed class UpdateWorkoutLogHandlerTests
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
        var handler = new UpdateWorkoutLogHandler(db, new PlateauDetectionService(new SeriesBuilder(db), db));

        var result = await handler.Handle(
            new UpdateWorkoutLogCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Notes",
                DateTime.UtcNow,
                DateTime.UtcNow,
                Array.Empty<UpdateWorkoutLogExerciseDto>()),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UpdatesLogExercisesAndSets_WhenLogExists()
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

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Leg Day",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);

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

        var start = DateTime.UtcNow.AddHours(-1);
        var end = DateTime.UtcNow;

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            StartedAt = start,
            CompletedAt = end,
            AiModified = false,
            Notes = "Original notes"
        };
        db.WorkoutLogs.Add(log);

        var oldSet = new WorkoutSetLog
        {
            Id = Guid.NewGuid(),
            LogId = log.Id,
            ExerciseId = exercise.Id,
            WorkoutExerciseId = null,
            SetId = null,
            Type = SetType.Normal,
            Reps = 5,
            Weight = 60,
            Duration = null,
            Distance = null,
            RestTime = 60,
            GroupNumber = 0,
            Rpe = 7,
            OrderIndex = 1,
            AiSuggested = false,
            LoggedAt = end
        };
        db.WorkoutLogSets.Add(oldSet);
        await db.SaveChangesAsync();

        var handler = new UpdateWorkoutLogHandler(db, new PlateauDetectionService(new SeriesBuilder(db), db));

        var updateCommand = new UpdateWorkoutLogCommand(
            userId,
            workout.Id,
            log.Id,
            "Updated notes",
            start,
            end,
            new List<UpdateWorkoutLogExerciseDto>
            {
                new UpdateWorkoutLogExerciseDto(
                    exercise.Id,
                    null,
                    1,
                    0,
                    new List<UpdateWorkoutLogSetDto>
                    {
                        new UpdateWorkoutLogSetDto(null, "Normal", 10, 100, null, null, 90, 8.5f, 1, 0),
                        new UpdateWorkoutLogSetDto(null, "Normal", 10, 105, null, null, 90, 9f, 2, 0)
                    }
                )
            });

        var result = await handler.Handle(updateCommand, CancellationToken.None);

        result.Should().BeTrue();

        var updatedLog = await db.WorkoutLogs.FirstAsync(l => l.Id == log.Id);
        updatedLog.Notes.Should().Be("Updated notes");
        updatedLog.StartedAt.Should().BeCloseTo(start, TimeSpan.FromMilliseconds(100));

        var updatedSets = await db.WorkoutLogSets.Where(s => s.LogId == log.Id).OrderBy(s => s.OrderIndex).ToListAsync();
        updatedSets.Should().HaveCount(2);
        updatedSets.Select(s => s.Weight).Should().ContainInOrder(100f, 105f);
        updatedSets.Select(s => s.Reps).Should().ContainInOrder(10, 10);
    }
}
