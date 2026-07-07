using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Profile;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Profile;

namespace OptiLifts.Tests.Unit.Profile;

public class GetProfileCalendarHandlerTests
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
    public async Task Handle_ReturnsLatestCompletedLogPerDayForRequestedMonth()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "calendar@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Calendar User"
        };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Calendar Workout",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc)
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
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
        };

        var juneEntry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = user.Id,
            Scheduled = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc),
            Status = ScheduleStatus.Scheduled
        };

        var secondWorkout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Evening Session",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc)
        };

        var secondEntry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = secondWorkout.Id,
            UserId = user.Id,
            Scheduled = new DateTime(2026, 6, 18, 17, 0, 0, DateTimeKind.Utc),
            Status = ScheduleStatus.Scheduled
        };

        var julyEntry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = user.Id,
            Scheduled = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc),
            Status = ScheduleStatus.Scheduled
        };

        context.Users.Add(user);
        context.Muscles.Add(muscle);
        context.Exercises.Add(exercise);
        context.Workouts.Add(workout);
        context.Workouts.Add(secondWorkout);
        await context.SaveChangesAsync();

        context.ScheduledEntries.AddRange(juneEntry, secondEntry, julyEntry);
        await context.SaveChangesAsync();

        var firstLog = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = juneEntry.Id,
            StartedAt = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc),
            AiModified = false
        };

        var latestLog = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = secondEntry.Id,
            StartedAt = new DateTime(2026, 6, 18, 18, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2026, 6, 18, 19, 15, 0, DateTimeKind.Utc),
            AiModified = false
        };

        context.WorkoutLogs.AddRange(firstLog, latestLog);
        await context.SaveChangesAsync();

        var handler = new GetProfileCalendarHandler(context);
        var result = await handler.Handle(new GetProfileCalendarQuery(user.Id, 2026, 6), CancellationToken.None);

        result.Entries.Should().ContainSingle();
        result.Entries[0].Date.Should().Be("2026-06-18");
        result.Entries[0].WorkoutId.Should().Be(secondWorkout.Id);
        result.Entries[0].LogId.Should().Be(latestLog.Id);
    }
}