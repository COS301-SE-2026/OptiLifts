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
    public async Task Handle_ReturnsOnlyRequestedMonthDates()
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
        await context.SaveChangesAsync();

        context.ScheduledEntries.AddRange(juneEntry, julyEntry);
        await context.SaveChangesAsync();

        var handler = new GetProfileCalendarHandler(context);
        var result = await handler.Handle(new GetProfileCalendarQuery(user.Id, 2026, 6), CancellationToken.None);

        result.HighlightedDates.Should().Equal("2026-06-18");
    }
}