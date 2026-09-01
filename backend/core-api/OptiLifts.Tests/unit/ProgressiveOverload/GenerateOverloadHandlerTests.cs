using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.ProgressiveOverload;
using OptiLifts.Domain.ProgressiveOverload;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.ProgressiveOverload;

namespace OptiLifts.Tests.ProgressiveOverload;

public class GenerateOverloadHandlerTests
{
    [Fact]
    public async Task Handle_FewerThanFourEligibleWorkouts_ReturnsNoDataPoints()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(testDb.Context, ExerciseType.WeightReps, null, null, [(100f, 10), (100f, 10), (100f, 10)]);
        var handler = new GenerateOverloadHandler(testDb.Context);

        var result = await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BodyweightExercise_AlwaysRecommendsOneMoreRepThanPreviousSession()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(testDb.Context, ExerciseType.BodyweightReps, "compound", null, [(0f, 10), (0f, 10), (0f, 10), (0f, 10)], userWeight: 80f);
        var handler = new GenerateOverloadHandler(testDb.Context);

        var result = await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        var expectedMetric = E1RMCalculator.CalculateE1RM(0f, 11, "compound", ExerciseType.BodyweightReps, 80f);
        result.Should().HaveCount(5);
        result[0].Metric.Should().BeApproximately(expectedMetric, 0.001d);

        var estimation = await testDb.Context.ExerciseEstimations.SingleAsync();
        estimation.Reps.Should().Be(11);
        estimation.Weight.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EligibleHistory_StoresCurrentExerciseEstimation()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(testDb.Context, ExerciseType.WeightReps, null, "barbell", [(100f, 10), (100f, 10), (100f, 10), (100f, 10)]);
        var handler = new GenerateOverloadHandler(testDb.Context);

        await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        var estimation = await testDb.Context.ExerciseEstimations.SingleAsync();
        estimation.UserId.Should().Be(setup.UserId);
        estimation.ExerciseId.Should().Be(setup.Exercise.Id);
        estimation.Weight.Should().Be(100f);
        estimation.Reps.Should().Be(11);
        estimation.ExerciseType.Should().Be(ExerciseType.WeightReps);
    }

    [Fact]
    public async Task Handle_ReversedRepsWithinRange_KeepsPreviousWeightAndRecommendsNewReps()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(testDb.Context, ExerciseType.WeightReps, null, "barbell", [(100f, 1), (100f, 1), (100f, 1), (100f, 1)]);
        var handler = new GenerateOverloadHandler(testDb.Context);

        var result = await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        var expectedMetric = E1RMCalculator.CalculateE1RM(100f, 13, null, ExerciseType.WeightReps);
        result.Should().HaveCount(5);
        result[0].Metric.Should().BeApproximately(expectedMetric, 0.001d);
    }

    [Fact]
    public async Task Handle_NonMachineExercise_TargetWeightIsRoundedDownToFiveKgIncrement()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(testDb.Context, ExerciseType.WeightReps, null, "dumbbell", [(100f, 17), (100f, 14), (100f, 11), (100f, 8)]);
        var handler = new GenerateOverloadHandler(testDb.Context);

        await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        var estimation = await testDb.Context.ExerciseEstimations.SingleAsync();
        estimation.Weight.Should().NotBeNull();
        estimation.Weight.Value.Should().BeGreaterThan(100f);
        ((estimation.Weight.Value - 100f) % 5f).Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public async Task Handle_MachineExercise_TruncatesPredictedTargetWeightToWholeNumber()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(testDb.Context, ExerciseType.WeightReps, null, "machine", [(100f, 13), (100f, 13), (100f, 13), (100f, 13)]);
        var handler = new GenerateOverloadHandler(testDb.Context);

        var result = await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        var lastActualMetric = E1RMCalculator.CalculateE1RM(100f, 13, null, ExerciseType.WeightReps);
        var predictedMetric = lastActualMetric * 1.02d;
        var expectedWeight = (float)Math.Truncate(E1RMCalculator.ReverseEpleyWeight(predictedMetric, 8, null, ExerciseType.WeightReps));
        var expectedMetric = E1RMCalculator.CalculateE1RM(expectedWeight, 8, null, ExerciseType.WeightReps);
        result[0].Metric.Should().BeApproximately(expectedMetric, 0.001d);
    }

    [Fact]
    public async Task Handle_WeightIncreaseIsNotPossible_ForcesRepIncreaseBeyondUpperLimit()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(testDb.Context, ExerciseType.WeightReps, null, "barbell", [(100f, 1), (90f, 1), (80f, 1), (70f, 1)]);
        var handler = new GenerateOverloadHandler(testDb.Context);

        var result = await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        var expectedMetric = E1RMCalculator.CalculateE1RM(100f, 13, null, ExerciseType.WeightReps);
        result[0].Metric.Should().BeApproximately(expectedMetric, 0.001d);
    }

    [Fact]
    public async Task Handle_GapGreaterThanFourteenDays_PreventsPrediction()
    {
        await using var testDb = await CreateTestDatabaseAsync();
        var setup = await SeedExerciseHistoryAsync(
            testDb.Context,
            ExerciseType.WeightReps,
            null,
            "barbell",
            [(100f, 10), (100f, 10), (100f, 10), (100f, 10)],
            [0, 7, 14, 30]);
        var handler = new GenerateOverloadHandler(testDb.Context);

        var result = await handler.Handle(new GenerateOverloadCommand(setup.UserId, setup.Exercise.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static async Task<TestDatabase> CreateTestDatabaseAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new OptiLiftsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new TestDatabase(connection, context);
    }

    private static async Task<(Guid UserId, Exercise Exercise)> SeedExerciseHistoryAsync(
        OptiLiftsDbContext context,
        ExerciseType exerciseType,
        string? mechanic,
        string? equipment,
        IReadOnlyList<(float Weight, int Reps)> loggedSets,
        IReadOnlyList<int>? daysAgo = null,
        float? userWeight = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            EmailHash = Guid.NewGuid().ToString(),
            PasswordHash = "test",
            DisplayName = "Test user",
            Weight = userWeight?.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var muscle = new Muscle { Id = Guid.NewGuid(), Name = $"Muscle {Guid.NewGuid()}" };
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test exercise",
            ExerciseType = exerciseType,
            Mechanic = mechanic,
            Equipment = equipment,
            PrimaryMuscleId = muscle.Id
        };
        var workout = new Workout { Id = Guid.NewGuid(), Name = "Test workout", CreatedBy = user.Id };

        context.AddRange(user, muscle, exercise, workout);

        for (int index = 0; index < loggedSets.Count; index++)
        {
            int offset = daysAgo?[index] ?? index * 7;
            var entry = new ScheduledEntry
            {
                Id = Guid.NewGuid(),
                WorkoutId = workout.Id,
                UserId = user.Id,
                Scheduled = DateTime.UtcNow.AddDays(-offset),
                Status = ScheduleStatus.Completed
            };
            var log = new WorkoutLog
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                StartedAt = DateTime.UtcNow.AddDays(-offset),
                CompletedAt = DateTime.UtcNow.AddDays(-offset).AddHours(1)
            };
            var set = new WorkoutSetLog
            {
                Id = Guid.NewGuid(),
                LogId = log.Id,
                ExerciseId = exercise.Id,
                Type = SetType.Normal,
                Weight = loggedSets[index].Weight,
                Reps = loggedSets[index].Reps,
                LoggedAt = log.CompletedAt.Value
            };

            context.AddRange(entry, log, set);
        }

        await context.SaveChangesAsync();
        return (user.Id, exercise);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        public TestDatabase(SqliteConnection connection, OptiLiftsDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        public SqliteConnection Connection { get; }
        public OptiLiftsDbContext Context { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}