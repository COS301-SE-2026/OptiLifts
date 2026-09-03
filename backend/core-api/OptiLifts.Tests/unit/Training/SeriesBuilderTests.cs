using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Tests.Unit.Training;

public class SeriesBuilderTests
{
    private static OptiLiftsDbContext makeContext(SqliteConnection connection)
    {
        var otps = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new OptiLiftsDbContext(otps);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<(Guid UserId, Guid ExerciseId, Guid WorkoutId)> SeedUserExerWorkoutAsync(
        OptiLiftsDbContext db, ExerciseType exerciseType, string mechanic, string? weight = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            PasswordHash = "x",
            DisplayName = "Series User",
            Weight = weight
        };

        var muscle = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };

        var exer = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            Mechanic = mechanic,
            Equipment = "barbell",
            PrimaryMuscleId = muscle.Id,
            ExerciseType = exerciseType
        };

        var workout = new Workout { Id = Guid.NewGuid(), Name = "Test Workout", CreatedBy = user.Id };

        db.Users.Add(user);
        db.Muscles.Add(muscle);
        db.Exercises.Add(exer);
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        return (user.Id, exer.Id, workout.Id);
    }

    private static async Task SeedSessAsync(
        OptiLiftsDbContext db, Guid userId, Guid workoutId, Guid exerciseId, DateTime completedAt,
        params (float Weight, int Reps, float? Rpe)[] sets)
    {
        var entry = new ScheduledEntry { Id = Guid.NewGuid(), WorkoutId = workoutId, UserId = userId, Scheduled = completedAt };
        var log = new WorkoutLog { Id = Guid.NewGuid(), EntryId = entry.Id, StartedAt = completedAt.AddMinutes(-45), CompletedAt = completedAt };

        db.ScheduledEntries.Add(entry);
        db.WorkoutLogs.Add(log);
        await db.SaveChangesAsync();

        var order = 0;
        foreach (var (weight, reps, rpe) in sets)
        {
            db.WorkoutLogSets.Add(new WorkoutSetLog
            {
                Id = Guid.NewGuid(),
                LogId = log.Id,
                ExerciseId = exerciseId,
                Type = SetType.Normal,
                Reps = reps,
                Weight = weight,
                Rpe = rpe,
                RestTime = 90,
                OrderIndex = order++
            });
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task BuildAsync_ReturnsEmptyForExerTypeUnsupported()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = makeContext(conn);

        var (userId, exerciseId, workoutId) = await SeedUserExerWorkoutAsync(db, ExerciseType.DistanceDuration, "compound");
        await SeedSessAsync(db, userId, workoutId, exerciseId, DateTime.UtcNow, (0, 1800, null));

        var builder = new SeriesBuilder(db);
        var result = await builder.BuildAsync(userId, exerciseId, null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ReturnsEmptyForExercDoesNotExist()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = makeContext(conn);

        var builder = new SeriesBuilder(db);
        var res = await builder.BuildAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        res.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ComputesMayhewE1rmCompoundExer()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = makeContext(conn);

        var (userId, exerciseId, workoutId) = await SeedUserExerWorkoutAsync(db, ExerciseType.WeightReps, "compound");
        await SeedSessAsync(db, userId, workoutId, exerciseId, DateTime.UtcNow, (100, 5, null));

        var builder = new SeriesBuilder(db);
        var res = await builder.BuildAsync(userId, exerciseId, null, CancellationToken.None);

        res.Should().HaveCount(1);
        res[0].E1rm.Should().BeApproximately(119.01f, 0.05f);
    }

    [Fact]
    public async Task BuildAsync_ComputesEpleyE1rmIsolationExer()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = makeContext(conn);

        var (userId, exerciseId, workoutId) = await SeedUserExerWorkoutAsync(db, ExerciseType.WeightReps, "isolation");
        await SeedSessAsync(db, userId, workoutId, exerciseId, DateTime.UtcNow, (40, 10, null));

        var builder = new SeriesBuilder(db);
        var res = await builder.BuildAsync(userId, exerciseId, null, CancellationToken.None);

        res.Should().HaveCount(1);
        res[0].E1rm.Should().BeApproximately(53.33f, 0.05f);
    }

    [Fact]
    public async Task BuildAsync_PicksBestSetE1rmMultipleSetsInSameSess()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = makeContext(conn);

        var (userId, exerciseId, workoutId) = await SeedUserExerWorkoutAsync(db, ExerciseType.WeightReps, "compound");
        await SeedSessAsync(db, userId, workoutId, exerciseId, DateTime.UtcNow,
            (80, 8, 7f), (100, 5, 9f), (60, 12, 6f));

        var builder = new SeriesBuilder(db);
        var res = await builder.BuildAsync(userId, exerciseId, null, CancellationToken.None);

        res.Should().HaveCount(1);
        res[0].E1rm.Should().BeApproximately(119.01f, 0.05f);
        res[0].SetCount.Should().Be(3);
        res[0].AvgRpe.Should().BeApproximately((7f + 9f + 6f) / 3f, 0.01f);
    }

    [Fact]
    public async Task BuildAsync_IgnoreZeroWeightSetsForWeightRepsExer()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = makeContext(conn);

        var (userId, exerciseId, workoutId) = await SeedUserExerWorkoutAsync(db, ExerciseType.WeightReps, "compound");
        await SeedSessAsync(db, userId, workoutId, exerciseId, DateTime.UtcNow, (0, 15, null), (100, 5, null));

        var builder = new SeriesBuilder(db);
        var res = await builder.BuildAsync(userId, exerciseId, null, CancellationToken.None);

        res.Should().HaveCount(1);
        res[0].E1rm.Should().BeApproximately(119.01f, 0.05f);
    }

    [Fact]
    public async Task BuildAsync_UsesCurrUserBodyweightWhenBodyweightRepsExer()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = makeContext(connection);

        var (userId, exerciseId, workoutId) = await SeedUserExerWorkoutAsync(db, ExerciseType.BodyweightReps, "compound", weight: "70");
        await SeedSessAsync(db, userId, workoutId, exerciseId, DateTime.UtcNow, (0, 8, null));

        var builder = new SeriesBuilder(db);
        var result = await builder.BuildAsync(userId, exerciseId, null, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].E1rm.Should().BeApproximately(88.40f, 0.05f);
    }

    [Fact]
    public async Task BuildAsync_RespectsSinceFilterAndOrdersByDateAsc()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = makeContext(conn);

        var (userId, exerciseId, workoutId) = await SeedUserExerWorkoutAsync(db, ExerciseType.WeightReps, "compound");
        var oldDate = DateTime.UtcNow.AddDays(-100);
        var recentDate = DateTime.UtcNow.AddDays(-10);
        var newestDate = DateTime.UtcNow.AddDays(-1);

        await SeedSessAsync(db, userId, workoutId, exerciseId, newestDate, (110, 5, null));
        await SeedSessAsync(db, userId, workoutId, exerciseId, oldDate, (90, 5, null));
        await SeedSessAsync(db, userId, workoutId, exerciseId, recentDate, (100, 5, null));

        var builder = new SeriesBuilder(db);
        var res = await builder.BuildAsync(userId, exerciseId, DateTime.UtcNow.AddDays(-30), CancellationToken.None);

        res.Should().HaveCount(2);
        res[0].Date.Date.Should().Be(recentDate.Date);
        res[1].Date.Date.Should().Be(newestDate.Date);
    }
}
