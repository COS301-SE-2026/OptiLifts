using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Training.GetPlateauPage;
using OptiLifts.Domain.Training;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Tests.Unit.Training;

public class GetPlateauPageHandlerTests
{
    private static async Task<OptiLiftsDbContext> NewDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var db = new OptiLiftsDbContext(new DbContextOptionsBuilder<OptiLiftsDbContext>().UseSqlite(conn).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task<Guid> SeedUserAsync(OptiLiftsDbContext db)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@x.com", EmailHash = Guid.NewGuid().ToString(), PasswordHash = "x", DisplayName = "U" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedTrendAsync(OptiLiftsDbContext db, Guid userId, TrendStatus status, bool rpeTrendRising, DateTime windowEnd)
    {
        var muscle = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };
        var exer = new Exercise { Id = Guid.NewGuid(), Name = "Bench Press", Mechanic = "compound", Equipment = "barbell", PrimaryMuscleId = muscle.Id, ExerciseType = ExerciseType.WeightReps };
        db.AddRange(muscle, exer);
        db.ExerciseTrends.Add(new ExerciseTrend
        {
            UserId = userId,
            ExerciseId = exer.Id,
            Status = status,
            RpeTrendRising = rpeTrendRising,
            WindowEnd = windowEnd,
            WindowStart = windowEnd.AddDays(-77),
            SessionsUsed = 12
        });
        await db.SaveChangesAsync();
        return exer.Id;
    }

    [Theory]
    [InlineData(TrendStatus.Progressing, false, null)]
    [InlineData(TrendStatus.Plateau, false, "Only your progress is stalling. Try changing this exercise or adjusting your rep range for a change of stimulus")]
    [InlineData(TrendStatus.Plateau, true, "Your effort has been climbing while progress has stalled. Prioritise sleep, nutrition and workout consistency before pushing harder on this exercise.")]
    [InlineData(TrendStatus.Regressing, false, "Only your progress is stalling. Try changing this exercise or adjusting your rep range for a change of stimulus")]
    public async Task Handle_BuildsExpectedRecom(TrendStatus status, bool rpeTrendRising, string? expected)
    {
        var db = await NewDbAsync();
        var userId = await SeedUserAsync(db);
        await SeedTrendAsync(db, userId, status, rpeTrendRising, DateTime.UtcNow);

        var handler = new GetPlateauPageHandler(db);
        var res = await handler.Handle(new GetPlateauPageQuery(userId), CancellationToken.None);

        res.Should().HaveCount(1);
        res[0].Recommendation.Should().Be(expected);
    }

    [Theory]
    [InlineData(TrendStatus.Plateau, false, true)]
    [InlineData(TrendStatus.Regressing, false, true)]
    [InlineData(TrendStatus.Plateau, true, false)]
    [InlineData(TrendStatus.Progressing, false, false)]
    public async Task Handle_ComputesCanSwapExer(TrendStatus status, bool rpeTrendRising, bool expectedCanSwap)
    {
        var db = await NewDbAsync();
        var userId = await SeedUserAsync(db);
        await SeedTrendAsync(db, userId, status, rpeTrendRising, DateTime.UtcNow);

        var handler = new GetPlateauPageHandler(db);
        var res = await handler.Handle(new GetPlateauPageQuery(userId), CancellationToken.None);

        res[0].CanSwapExercise.Should().Be(expectedCanSwap);
    }

    [Fact]
    public async Task Handle_ExcldsTrendsOlderThanRecCutoff()
    {
        var db = await NewDbAsync();
        var userId = await SeedUserAsync(db);
        await SeedTrendAsync(db, userId, TrendStatus.Plateau, false, DateTime.UtcNow.AddDays(-45)); // beyond 30-day cutoff

        var handler = new GetPlateauPageHandler(db);
        var res = await handler.Handle(new GetPlateauPageQuery(userId), CancellationToken.None);

        res.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcldsOtherUsersTrends()
    {
        var theDb = await NewDbAsync();
        var userId = await SeedUserAsync(theDb);
        var otherUserId = await SeedUserAsync(theDb);
        await SeedTrendAsync(theDb, otherUserId, TrendStatus.Plateau, false, DateTime.UtcNow);

        var handler = new GetPlateauPageHandler(theDb);
        var res = await handler.Handle(new GetPlateauPageQuery(userId), CancellationToken.None);

        res.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ListsWorkoutsOwnedByUserContainingThatExer()
    {
        var db = await NewDbAsync();
        var userId = await SeedUserAsync(db);
        var otherUserId = await SeedUserAsync(db);
        var exerId = await SeedTrendAsync(db, userId, TrendStatus.Plateau, false, DateTime.UtcNow);

        var ownWorkout = new Workout { Id = Guid.NewGuid(), Name = "My Push Day", CreatedBy = userId };
        var otherUsersWorkout = new Workout { Id = Guid.NewGuid(), Name = "Someone Else's", CreatedBy = otherUserId };
        db.AddRange(ownWorkout, otherUsersWorkout);
        await db.SaveChangesAsync();

        db.WorkoutExercises.AddRange(
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = ownWorkout.Id, ExerciseId = exerId, OrderIndex = 0 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = otherUsersWorkout.Id, ExerciseId = exerId, OrderIndex = 0 });
        await db.SaveChangesAsync();

        var handler = new GetPlateauPageHandler(db);
        var res = await handler.Handle(new GetPlateauPageQuery(userId), CancellationToken.None);

        res[0].Workouts.Should().ContainSingle(w => w.WorkoutName == "My Push Day");
    }
}
