using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Domain.Training;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Tests.Unit.Training;

public class PlateauDetectionServiceTests
{
    private static async Task<OptiLiftsDbContext> NewDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var db = new OptiLiftsDbContext(new DbContextOptionsBuilder<OptiLiftsDbContext>().UseSqlite(conn).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task<(Guid UserId, Guid ExerciseId)> SeedUserAndExerAsync(OptiLiftsDbContext db)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@example.com", PasswordHash = "x", DisplayName = "Plateau User" };
        var muscle = new Muscle { Id = Guid.NewGuid(), Name = "Back" };
        var exer = new Exercise { Id = Guid.NewGuid(), Name = "Test Exercise", Mechanic = "compound", Equipment = "barbell", PrimaryMuscleId = muscle.Id, ExerciseType = ExerciseType.WeightReps };
        db.AddRange(user, muscle, exer);
        await db.SaveChangesAsync();
        return (user.Id, exer.Id);
    }

    private static List<SeriesPoint> Weekly(DateTime start, int count, float startE1rm, float weeklyPct, float? rpeStart = null, float rpeWeeklyDelta = 0) =>
        Enumerable.Range(0, count).Select(i => new SeriesPoint(
            Guid.NewGuid(), start.AddDays(i * 7),
            (float)(startE1rm * Math.Pow(1 + weeklyPct / 100.0, i)),
            rpeStart.HasValue ? rpeStart + rpeWeeklyDelta * i : null, 0, 3)).ToList();

    private static List<SeriesPoint> BaselineThenWindow(DateTime start, float windowWeeklyPct)
    {
        var baseline = Weekly(start, 9, 100f, 2f);
        var window = Weekly(start.AddDays(56), 13, baseline[^1].E1rm, windowWeeklyPct).Skip(1).ToList();
        return baseline.Concat(window).ToList();
    }

    private static PlateauDetectionService NewService(OptiLiftsDbContext db, Guid userId, Guid exerciseId, List<SeriesPoint> series)
    {
        var mock = new Mock<ISeriesBuilder>();
        mock.Setup(s => s.BuildAsync(userId, exerciseId, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(series);
        return new PlateauDetectionService(mock.Object, db);
    }

    [Theory]
    [InlineData(2f, TrendStatus.Progressing)]
    [InlineData(0f, TrendStatus.InsufficientBaseline)]
    [InlineData(-2f, TrendStatus.Regressing)]
    public async Task DetectAsync_ClassifiesWithNoBaseline(float weeklyPct, TrendStatus expected)
    {
        var database = await NewDbAsync();
        var (userId, exerciseId) = await SeedUserAndExerAsync(database);
        var series = Weekly(DateTime.UtcNow.AddDays(-77), 12, 100f, weeklyPct);

        var srvce = NewService(database, userId, exerciseId, series);
        await srvce.DetectAsync(userId, exerciseId, CancellationToken.None);

        (await database.ExerciseTrends.SingleAsync()).Status.Should().Be(expected);
    }

    [Theory]
    [InlineData(0f, TrendStatus.Plateau)]
    [InlineData(-3f, TrendStatus.Regressing)]
    public async Task DetectAsync_ClassifiesAgainstRisingBaseline(float windowWeeklyPct, TrendStatus expected)
    {
        var theDb = await NewDbAsync();
        var (userId, exerciseId) = await SeedUserAndExerAsync(theDb);
        var series = BaselineThenWindow(DateTime.UtcNow.AddDays(-140), windowWeeklyPct);

        var srvce = NewService(theDb, userId, exerciseId, series);
        await srvce.DetectAsync(userId, exerciseId, CancellationToken.None);

        (await theDb.ExerciseTrends.SingleAsync()).Status.Should().Be(expected);
    }

    [Fact]
    public async Task DetectAsync_SetsInsufficientDataFewerThanWindowSize()
    {
        var db = await NewDbAsync();
        var (userId, exerciseId) = await SeedUserAndExerAsync(db);
        var series = Weekly(DateTime.UtcNow.AddDays(-30), 5, 100f, 0f);

        var srvce = NewService(db, userId, exerciseId, series);
        await srvce.DetectAsync(userId, exerciseId, CancellationToken.None);

        (await db.ExerciseTrends.SingleAsync()).Status.Should().Be(TrendStatus.InsufficientData);
    }

    [Fact]
    public async Task DetectAsync_SetsInsufficientDataGapExceedsMaxGap()
    {
        var db = await NewDbAsync();
        var (userId, exerciseId) = await SeedUserAndExerAsync(db);
        var startTime = DateTime.UtcNow.AddDays(-120);
        var series = Weekly(startTime, 6, 100f, 0f).Concat(Weekly(startTime.AddDays(55), 6, 100f, 0f)).ToList();

        var srvce = NewService(db, userId, exerciseId, series);
        await srvce.DetectAsync(userId, exerciseId, CancellationToken.None);

        (await db.ExerciseTrends.SingleAsync()).Status.Should().Be(TrendStatus.InsufficientData);
    }

    [Fact]
    public async Task DetectAsync_SetsRpeTrendRising_RPEClimbsFasterThanThresh()
    {
        var theDb = await NewDbAsync();
        var (userId, exerciseId) = await SeedUserAndExerAsync(theDb);
        var series = Weekly(DateTime.UtcNow.AddDays(-77), 12, 100f, 2f, rpeStart: 6f, rpeWeeklyDelta: 0.5f); // > 0.3/wk threshold

        var srvce = NewService(theDb, userId, exerciseId, series);
        await srvce.DetectAsync(userId, exerciseId, CancellationToken.None);

        (await theDb.ExerciseTrends.SingleAsync()).RpeTrendRising.Should().BeTrue();
    }

    [Fact]
    public async Task DetectAsync_RecordsPlateauEventOnlyOnceConfirmedAndCooldown()
    {
        var db = await NewDbAsync();
        var (userId, exerciseId) = await SeedUserAndExerAsync(db);
        var series = BaselineThenWindow(DateTime.UtcNow.AddDays(-140), windowWeeklyPct: 0f);
        var service = NewService(db, userId, exerciseId, series);

        await service.DetectAsync(userId, exerciseId, CancellationToken.None); // 1st: freshly created, not "confirmed" yet
        await service.DetectAsync(userId, exerciseId, CancellationToken.None); // 2nd: same status as stored -> confirmed -> event recorded
        await service.DetectAsync(userId, exerciseId, CancellationToken.None); // 3rd: within cooldown -> no duplicate

        (await db.TrainingEvents.CountAsync(e => e.Type == TrainingEventType.PlateauDetected)).Should().Be(1);
    }
}
