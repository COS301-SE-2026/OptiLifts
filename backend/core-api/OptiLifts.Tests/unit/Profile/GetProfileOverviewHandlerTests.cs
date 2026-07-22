using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Profile;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Profile;

namespace OptiLifts.Tests.Unit.Profile;

public class GetProfileOverviewHandlerTests
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
    public async Task Handle_WithCompletedSession_ReturnsProfileOverview()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "profile@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Profile User",
            Bio = "Built for testing",
            CreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Push Day",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc)
        };

        var muscle = new Muscle
        {
            Id = Guid.NewGuid(),
            Name = "Chest"
        };

        var badge = new OptiLifts.Domain.Gamification.Badge
        {
            Id = Guid.NewGuid(),
            Code = "workout_count",
            Name = "First Workout",
            Description = "Complete your first workout",
            Category = OptiLifts.Domain.Gamification.BadgeCategory.Milestone,
            Threshold = 1,
            CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var entry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = user.Id,
            Scheduled = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc),
            Status = ScheduleStatus.Completed
        };

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        var workoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            OrderIndex = 0
        };

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            StartedAt = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2026, 6, 18, 9, 5, 0, DateTimeKind.Utc),
            AiModified = false,
            Notes = "test run"
        };

        context.Users.Add(user);
        context.Workouts.Add(workout);
        context.Muscles.Add(muscle);
        context.Badges.Add(badge);
        context.Exercises.Add(exercise);

        await context.SaveChangesAsync();

        context.ScheduledEntries.Add(entry);
        context.WorkoutExercises.Add(workoutExercise);
        await context.SaveChangesAsync();

        context.WorkoutLogs.Add(log);
        await context.SaveChangesAsync();

        context.UserBadges.Add(new OptiLifts.Domain.Gamification.UserBadge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BadgeId = badge.Id,
            EarnedAt = new DateTime(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc)
        });

        await context.SaveChangesAsync();

        context.WorkoutLogSets.Add(new WorkoutSetLog
        {
            Id = Guid.NewGuid(),
            LogId = log.Id,
            ExerciseId = exercise.Id,
            WorkoutExerciseId = workoutExercise.Id,
            Type = SetType.Normal,
            Reps = 8,
            Weight = 100,
            Duration = null,
            Distance = null,
            RestTime = 120,
            GroupNumber = 0,
            Rpe = 8,
            OrderIndex = 0,
            AiSuggested = false,
            LoggedAt = new DateTime(2026, 6, 18, 8, 10, 0, DateTimeKind.Utc)
        });

        await context.SaveChangesAsync();

        var handler = new GetProfileOverviewHandler(context);
        var result = await handler.Handle(new GetProfileOverviewQuery(user.Id), CancellationToken.None);

        result.Profile.Name.Should().Be("Profile User");
        result.Profile.Email.Should().Be("profile@example.com");
        result.Profile.Bio.Should().Be("Built for testing");
        result.Stats.Should().HaveCount(3);
        result.Badges.Should().HaveCount(1);
        result.Badges[0].Name.Should().Be("First Workout");
        result.RecentWorkouts.Should().HaveCount(1);
        result.RecentWorkouts[0].Name.Should().Be("Push Day");
        result.ChartData.Should().HaveCount(12);
        result.ChartTitle.Should().Be("Workout volume");
    }

    [Fact]
    public async Task Handle_WithVeryShortCompletedSession_FormatsDurationAsLessThanOneMinute()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "profile@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Profile User",
            Bio = "Built for testing",
            CreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Quick Circuit",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc)
        };

        var entry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = user.Id,
            Scheduled = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc),
            Status = ScheduleStatus.Completed
        };

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            StartedAt = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2026, 6, 18, 8, 0, 30, DateTimeKind.Utc),
            AiModified = false,
            Notes = "quick run"
        };

        context.Users.Add(user);
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        context.ScheduledEntries.Add(entry);
        await context.SaveChangesAsync();

        context.WorkoutLogs.Add(log);
        await context.SaveChangesAsync();

        var handler = new GetProfileOverviewHandler(context);
        var result = await handler.Handle(new GetProfileOverviewQuery(user.Id), CancellationToken.None);

        result.RecentWorkouts.Should().ContainSingle();
        result.RecentWorkouts[0].Duration.Should().Be("<1m");
    }

    [Fact]
    public async Task Handle_WithoutCompletedSessions_DoesNotReturnPlannedWorkouts()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "profile@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Profile User",
            Bio = "Built for testing",
            CreatedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Push Day",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc)
        };

        context.Users.Add(user);
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        var handler = new GetProfileOverviewHandler(context);
        var result = await handler.Handle(new GetProfileOverviewQuery(user.Id), CancellationToken.None);

        result.RecentWorkouts.Should().BeEmpty();
        result.Stats.Should().ContainSingle(stat => stat.Label == "Streak" && stat.Value == "0 weeks");
        result.Stats.Should().ContainSingle(stat => stat.Label == "Workouts" && stat.Value == "0 sessions");
        result.Stats.Should().ContainSingle(stat => stat.Label == "Records" && stat.Value == "0 logged sets");
    }
}