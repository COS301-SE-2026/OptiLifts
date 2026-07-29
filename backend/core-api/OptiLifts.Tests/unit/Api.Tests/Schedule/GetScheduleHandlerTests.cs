using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
namespace OptiLifts.Tests.Api.Tests;

public sealed class GetScheduleHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmpty_NoScheduleEntries()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var handler = new GetScheduleHandler(db);

        var result = await handler.Handle(
            new GetScheduleQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(-1)), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnEntries_WithWorkouts()
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
            Email = "test@example.com",
            PasswordHash = "x",
            DisplayName = "Test person"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        //updated tests after updating getschedule to include workout summaries
        var chest = new Muscle
        {
            Name = "Chest"
        };
        db.Muscles.Add(chest);
        await db.SaveChangesAsync();

        var exercise = new Exercise
        {
            Name = "Bench",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = chest.Id
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Hypertrophy",
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

        var set1 = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            Reps = 10,
            Weight = 100,
            OrderIndex = 0
        };
        var set2 = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            Reps = 10,
            Weight = 120,
            OrderIndex = 1
        };
        db.Sets.Add(set1);
        db.Sets.Add(set2);
        await db.SaveChangesAsync();

        var schedule = new DateTime(2026, 6, 27, 10, 0, 0, DateTimeKind.Utc);
        var entry = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = schedule,
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new GetScheduleHandler(db);
        var result = await handler.Handle(
            new GetScheduleQuery(userId, new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 28, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        result.Should().HaveCount(1);
        var first = result[0];
        first.Id.Should().Be(entry.Id);
        first.WorkoutId.Should().Be(workout.Id);
        first.WorkoutName.Should().Be("Hypertrophy");
        first.Scheduled.Should().Be(schedule);
        first.Status.Should().Be("Scheduled");

        //extra after endpoint update
        first.PrimaryMuscleGroups.Should().Contain("Chest");
        first.ExerciseCount.Should().Be(1);
        first.ExercisePreview.Should().Contain("Bench");
        first.TotalVolume.Should().Be(2200);
        first.TotalSets.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ReturnsPrCount_ForCompletedWorkout()
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
            Email = "completed@example.com",
            PasswordHash = "x",
            DisplayName = "Completed Person"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var chest = new Muscle { Name = "Chest" };
        db.Muscles.Add(chest);
        await db.SaveChangesAsync();

        var exercise = new Exercise
        {
            Name = "Bench",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = chest.Id
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Hypertrophy",
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

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = Guid.NewGuid(),
            StartedAt = new DateTime(2026, 6, 27, 8, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2026, 6, 27, 9, 0, 0, DateTimeKind.Utc),
            AiModified = false
        };
        db.ScheduledEntries.Add(new ScheduledEntry
        {
            Id = log.EntryId ?? Guid.NewGuid(),
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = new DateTime(2026, 6, 27, 10, 0, 0, DateTimeKind.Utc),
            Status = ScheduleStatus.Completed
        });
        db.WorkoutLogs.Add(log);
        await db.SaveChangesAsync();

        var loggedSet = new WorkoutSetLog
        {
            Id = Guid.NewGuid(),
            LogId = log.Id,
            ExerciseId = exercise.Id,
            WorkoutExerciseId = workoutExercise.Id,
            Type = SetType.Normal,
            Reps = 8,
            Weight = 100,
            RestTime = 90,
            GroupNumber = 0,
            Rpe = 8,
            OrderIndex = 0,
            AiSuggested = false,
            LoggedAt = DateTime.UtcNow
        };
        db.WorkoutLogSets.Add(loggedSet);
        await db.SaveChangesAsync();

        db.ExercisePrs.AddRange(
            new ExercisePr
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExerciseId = exercise.Id,
                WorkoutLogSetId = loggedSet.Id,
                PrType = ExercisePrType.MaxWeight,
                PrValue = 100,
                AchievedWeight = 100,
                AchievedReps = 8
            },
            new ExercisePr
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExerciseId = exercise.Id,
                WorkoutLogSetId = loggedSet.Id,
                PrType = ExercisePrType.MaxSetVolume,
                PrValue = 800,
                AchievedWeight = 100,
                AchievedReps = 8
            });
        await db.SaveChangesAsync();

        var handler = new GetScheduleHandler(db);
        var result = await handler.Handle(
            new GetScheduleQuery(userId, new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 28, 0, 0, 0, DateTimeKind.Utc), ScheduleStatus.Completed), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].RecordCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_FiltersEntries_OutsideDateRange()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;
        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "x",
            DisplayName = "Test person"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Cardio",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var schedOutside = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(schedOutside);
        await db.SaveChangesAsync();

        var schedInside = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = new DateTime(2026, 6, 27, 10, 0, 0, DateTimeKind.Utc),
        };
        db.ScheduledEntries.Add(schedInside);
        await db.SaveChangesAsync();

        var handler = new GetScheduleHandler(db);
        var result = await handler.Handle(new GetScheduleQuery(userId, new DateTime(2026, 6, 25, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 28, 20, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(schedInside.Id);
    }

    [Fact]
    public async Task Handle_CurrentWeekDefaults_WhenNoQueryDates()
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
            Email = "finaltest@example.com",
            PasswordHash = "y",
            DisplayName = "Final Test"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Push",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var schedule = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.Date.AddHours(12),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(schedule);
        await db.SaveChangesAsync();

        var nextsched = new ScheduledEntry
        {
            UserId = userId,
            WorkoutId = workout.Id,
            Scheduled = DateTime.UtcNow.Date.AddYears(1),
            Status = ScheduleStatus.Scheduled
        };
        db.ScheduledEntries.Add(nextsched);
        await db.SaveChangesAsync();

        var handler = new GetScheduleHandler(db);
        var result = await handler.Handle(new GetScheduleQuery(userId, StartDate: null, EndDate: null), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(schedule.Id);
    }
}