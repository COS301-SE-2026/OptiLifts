using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetScheduleAnalytics;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
using OptiLifts.Domain.Users;
namespace OptiLifts.Tests.Api.Tests;

public sealed class GetScheduleAnalyticsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmptyAnalytics_NoScheduled()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var handler = new GetScheduleAnalyticsHandler(db);
        var result = await handler.Handle(new GetScheduleAnalyticsQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1)), CancellationToken.None);

        result.TotalWorkouts.Should().Be(0);
        result.TotalVolume.Should().Be(0);
        result.TotalSets.Should().Be(0);
        result.MuscleDistribution.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsMetrics_andMuscleDistribution()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "testingmystuff@example.com",
            PasswordHash = "y",
            DisplayName = "Test mense"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var chest = new Muscle
        {
            Name = "Upper Back"
        };
        db.Muscles.Add(chest);
        await db.SaveChangesAsync();

        var exercise = new Exercise
        {
            Name = "Lat Pulldown",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = chest.Id
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Push Dayy",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var workoutExercise = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            OrderIndex = 0
        };
        db.WorkoutExercises.Add(workoutExercise);
        await db.SaveChangesAsync();

        var setOne = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            Reps = 12,
            Weight = 40,
            OrderIndex = 0
        };
        var setTwo = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            Reps = 8,
            Weight = 60,
            OrderIndex = 1
        };
        var setThree = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            Reps = 9,
            Weight = 60,
            OrderIndex = 2
        };
        db.Sets.AddRange(setOne, setTwo, setThree);
        await db.SaveChangesAsync();

        var schedule = new DateTime(2026,6,27,10,0,0, DateTimeKind.Utc);
        var entry = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = schedule,
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new GetScheduleAnalyticsHandler(db);
        var result = await handler.Handle(
            new GetScheduleAnalyticsQuery(userId, new DateTime(2026,6,25,0,0,0, DateTimeKind.Utc), new DateTime(2026,6,28,0,0,0, DateTimeKind.Utc)), CancellationToken.None);

        result.TotalWorkouts.Should().Be(1);
        result.TotalVolume.Should().Be(1500);
        result.TotalSets.Should().Be(3);
        result.MuscleDistribution.Should().HaveCount(1);
        result.MuscleDistribution[0].MuscleGroup.Should().Be("Upper Back");
        result.MuscleDistribution[0].SetCount.Should().Be(3);
        result.MuscleDistribution[0].Percentage.Should().Be(100f);
    }

    [Fact]
    public async Task Handle_FiltersStatus_WhenStatusGiven()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "superdupertests@tests.com",
            PasswordHash = "v",
            DisplayName = "MyGreatTests"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var chest = new Muscle
        {
            Name = "Quadriceps"
        };
        db.Muscles.Add(chest);
        await db.SaveChangesAsync();

        var exercise = new Exercise
        {
            Name = "Squat",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = chest.Id
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "LegsQueen",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var workoutExercise = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            OrderIndex = 0
        };
        db.WorkoutExercises.Add(workoutExercise);
        await db.SaveChangesAsync();

        var set = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            Reps = 10,
            Weight = 200,
            OrderIndex = 0
        };
        db.Sets.Add(set);

        var entryDone = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = new DateTime(2026,6,26,9,0,0, DateTimeKind.Utc),
            Status = ScheduleStatus.Completed
        };
        db.ScheduledEntries.Add(entryDone);
        await db.SaveChangesAsync();

        var schedule = new DateTime(2026,6,27,11,0,0, DateTimeKind.Utc);
        var entrySched = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = schedule,
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entrySched);
        await db.SaveChangesAsync();

        var handler = new GetScheduleAnalyticsHandler(db);
        var query = new GetScheduleAnalyticsQuery(userId, 
            new DateTime(2026,6,25,0,0,0, DateTimeKind.Utc), 
            new DateTime(2026,6,28,0,0,0, DateTimeKind.Utc), ScheduleStatus.Completed);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalWorkouts.Should().Be(1);
        result.TotalVolume.Should().Be(2000f);
        result.TotalSets.Should().Be(1);
    }
}