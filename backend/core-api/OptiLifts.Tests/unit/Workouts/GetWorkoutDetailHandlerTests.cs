using System.Linq;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Unit.Workouts;

public class GetWorkoutDetailHandlerTests
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
    public async Task Handle_ReturnsWorkoutDetail_WithDistanceDurationExerciseAndSummaryData()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "detail@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Detail User"
        };

        var quadriceps = new Muscle { Id = Guid.NewGuid(), Name = "Quadriceps" };
        var chest = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Full Body",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc)
        };

        var running = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Running",
            Mechanic = "compound",
            Equipment = "bodyweight",
            PrimaryMuscleId = quadriceps.Id,
            ExerciseType = ExerciseType.DistanceDuration
        };

        var bench = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = chest.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        context.Users.Add(user);
        context.Muscles.AddRange(quadriceps, chest);
        context.Workouts.Add(workout);
        context.Exercises.AddRange(running, bench);
        await context.SaveChangesAsync();

        var runningWorkoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = running.Id,
            OrderIndex = 0
        };

        var benchWorkoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = bench.Id,
            OrderIndex = 1
        };

        context.WorkoutExercises.AddRange(runningWorkoutExercise, benchWorkoutExercise);
        await context.SaveChangesAsync();

        context.Sets.AddRange(
            new WorkoutSet
            {
                Id = Guid.NewGuid(),
                WorkoutExerciseId = runningWorkoutExercise.Id,
                Type = SetType.Normal,
                Reps = 1800,
                Duration = 900,
                Distance = 5,
                OrderIndex = 0,
                RestTime = 90
            },
            new WorkoutSet
            {
                Id = Guid.NewGuid(),
                WorkoutExerciseId = benchWorkoutExercise.Id,
                Type = SetType.Normal,
                Reps = 8,
                Weight = 80,
                OrderIndex = 0,
                RestTime = 120
            });

        await context.SaveChangesAsync();

        var handler = new GetWorkoutDetailHandler(context);
        var result = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(workout.Id);
        result.Name.Should().Be("Full Body");
        result.PrimaryMuscleGroups.Should().ContainInOrder("Quadriceps", "Chest");
        result.ExercisePreview.Should().ContainInOrder("Running", "Bench Press");
        result.Exercises.Should().HaveCount(2);

        var runningResult = result.Exercises[0];
        runningResult.Name.Should().Be("Running");
        runningResult.PrimaryMuscle.Should().Be("Quadriceps");
        runningResult.ExerciseType.Should().Be("DistanceDuration");
        runningResult.Sets.Should().HaveCount(1);
        runningResult.Sets[0].Duration.Should().Be(900);
        runningResult.Sets[0].Distance.Should().Be(5);

        var benchResult = result.Exercises[1];
        benchResult.Name.Should().Be("Bench Press");
        benchResult.Sets[0].Weight.Should().Be(80);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenWorkoutDoesNotBelongToUser()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@example.com",
            EmailHash = "owner-hash",
            PasswordHash = "passwordhash",
            DisplayName = "Owner"
        };

        var otherUserId = Guid.NewGuid();
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Owner Workout",
            CreatedBy = owner.Id
        };

        context.Users.Add(owner);
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        var handler = new GetWorkoutDetailHandler(context);
        var result = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, otherUserId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TimeConstrained_WithBudget_DropsSetsAndReducesRest()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "budget@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Budget User"
        };

        var chest = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Time Restricted Workout",
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var bench = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = chest.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        context.Users.Add(user);
        context.Muscles.Add(chest);
        context.Workouts.Add(workout);
        context.Exercises.Add(bench);
        await context.SaveChangesAsync();

        var benchWorkoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = bench.Id,
            OrderIndex = 0
        };

        context.WorkoutExercises.Add(benchWorkoutExercise);
        await context.SaveChangesAsync();

        // 3 sets, 10 reps (40s), 300s rest each = 1020s total
        context.Sets.AddRange(
            new WorkoutSet { Id = Guid.NewGuid(), WorkoutExerciseId = benchWorkoutExercise.Id, Type = SetType.Normal, Reps = 10, Weight = 80, OrderIndex = 0, RestTime = 300 },
            new WorkoutSet { Id = Guid.NewGuid(), WorkoutExerciseId = benchWorkoutExercise.Id, Type = SetType.Normal, Reps = 10, Weight = 80, OrderIndex = 1, RestTime = 300 },
            new WorkoutSet { Id = Guid.NewGuid(), WorkoutExerciseId = benchWorkoutExercise.Id, Type = SetType.Normal, Reps = 10, Weight = 80, OrderIndex = 2, RestTime = 300 }
        );
        await context.SaveChangesAsync();

        var handler = new GetWorkoutDetailHandler(context);

        // 5 minute budget = 300 seconds
        var result = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 5), CancellationToken.None);

        result.Should().NotBeNull();
        result.Exercises.Should().HaveCount(1);

        var sets = result.Exercises[0].Sets;
        sets.Should().NotBeEmpty();

        // Total time should be <= 360 seconds (5 min + 1 min margin)
        var totalTime = 0;
        foreach (var s in sets)
        {
            totalTime += ((s.Reps ?? 10) * 4) + s.RestTime;
        }
        totalTime.Should().BeLessThanOrEqualTo(360);
    }

    [Fact]
    public async Task Handle_TimeConstrained_BodyweightWorkout_PreservesBodyweightExerciseType()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "bw@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "BW User"
        };

        var back = new Muscle { Id = Guid.NewGuid(), Name = "Back" };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Bodyweight Workout",
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var pullups = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Pull-Ups",
            Mechanic = "compound",
            Equipment = "bodyweight",
            PrimaryMuscleId = back.Id,
            ExerciseType = ExerciseType.BodyweightReps
        };

        var barbellRow = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Barbell Row",
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = back.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        context.Users.Add(user);
        context.Muscles.Add(back);
        context.Workouts.Add(workout);
        context.Exercises.AddRange(pullups, barbellRow);
        await context.SaveChangesAsync();

        var bwWorkoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = pullups.Id,
            OrderIndex = 0
        };

        context.WorkoutExercises.Add(bwWorkoutExercise);
        await context.SaveChangesAsync();

        var handler = new GetWorkoutDetailHandler(context);

        var result = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 30), CancellationToken.None);

        result.Should().NotBeNull();
        result.Exercises.Should().HaveCount(1);
        result.Exercises[0].ExerciseType.Should().Be("BodyweightReps");
    }

    [Fact]
    public async Task Handle_TimeConstrained_DifferentBudgets_ProduceDifferentWorkoutDurationsAndSets()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "budgets@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Budgets User"
        };

        var chest = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };
        var shoulders = new Muscle { Id = Guid.NewGuid(), Name = "Shoulders" };
        var triceps = new Muscle { Id = Guid.NewGuid(), Name = "Triceps" };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Push Day",
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var bench = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = chest.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        var overheadPress = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Overhead Press",
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = shoulders.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        var dips = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Dips",
            Mechanic = "compound",
            Equipment = "bodyweight",
            PrimaryMuscleId = triceps.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        context.Users.Add(user);
        context.Muscles.AddRange(chest, shoulders, triceps);
        context.Workouts.Add(workout);
        context.Exercises.AddRange(bench, overheadPress, dips);
        await context.SaveChangesAsync();

        context.WorkoutExercises.AddRange(
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = bench.Id, OrderIndex = 0 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = overheadPress.Id, OrderIndex = 1 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = dips.Id, OrderIndex = 2 }
        );
        await context.SaveChangesAsync();

        var handler = new GetWorkoutDetailHandler(context);

        // 10 minutes budget vs 45 minutes budget
        var resultShort = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 10), CancellationToken.None);
        var resultLong = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 45), CancellationToken.None);

        resultShort.Should().NotBeNull();
        resultLong.Should().NotBeNull();

        int shortTime = resultShort.Exercises.Sum(e => e.Sets.Sum(s => ((s.Reps ?? 10) * 4) + s.RestTime));
        int longTime = resultLong.Exercises.Sum(e => e.Sets.Sum(s => ((s.Reps ?? 10) * 4) + s.RestTime));

        shortTime.Should().BeLessThanOrEqualTo(10 * 60 + 60);
        longTime.Should().BeGreaterThan(shortTime);
    }

    [Fact]
    public async Task Handle_TimeConstrained_PrioritizesMostTargetedMuscleInOriginalWorkout()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "priority@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Priority User"
        };

        var chest = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };
        var biceps = new Muscle { Id = Guid.NewGuid(), Name = "Biceps" };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Chest Heavy Day",
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var bench = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            Mechanic = "compound",
            Equipment = "barbell",
            PrimaryMuscleId = chest.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        var inclinePress = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Incline Dumbbell Press",
            Mechanic = "compound",
            Equipment = "dumbbell",
            PrimaryMuscleId = chest.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        var chinUp = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Chin-Ups",
            Mechanic = "compound",
            Equipment = "bodyweight",
            PrimaryMuscleId = biceps.Id,
            ExerciseType = ExerciseType.WeightReps
        };

        context.Users.Add(user);
        context.Muscles.AddRange(chest, biceps);
        context.Workouts.Add(workout);
        context.Exercises.AddRange(bench, inclinePress, chinUp);
        await context.SaveChangesAsync();

        // 2 Chest exercises vs 1 Bicep exercise -> Chest is heavily prioritized
        context.WorkoutExercises.AddRange(
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = bench.Id, OrderIndex = 0 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = inclinePress.Id, OrderIndex = 1 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = chinUp.Id, OrderIndex = 2 }
        );
        await context.SaveChangesAsync();

        var handler = new GetWorkoutDetailHandler(context);

        // Budget of 10 min -> fits 1 exercise
        var result = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 10), CancellationToken.None);

        result.Should().NotBeNull();
        result.Exercises.Should().NotBeEmpty();
        // The first and highest priority exercise must target Chest (top muscle group)
        result.Exercises[0].PrimaryMuscle.Should().Be("Chest");

        // Budget of 15 min -> fits 2 exercises
        // Must contain Chest (top muscle group) AND Biceps (other muscle group in workout), not 2 Chest exercises
        var result15 = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 15), CancellationToken.None);
        result15.Should().NotBeNull();
        result15.Exercises.Length.Should().Be(2);
        result15.Exercises.Should().Contain(e => e.PrimaryMuscle == "Chest");
        result15.Exercises.Should().Contain(e => e.PrimaryMuscle == "Biceps");
    }

    [Fact]
    public async Task Handle_TimeConstrained_15MinCappedTo2ExercisesWith2Sets_And30MinCappedTo3ExercisesWith3Sets()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "caps@example.com",
            EmailHash = "hash",
            PasswordHash = "passwordhash",
            DisplayName = "Caps User"
        };

        var chest = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };
        var shoulders = new Muscle { Id = Guid.NewGuid(), Name = "Shoulders" };
        var triceps = new Muscle { Id = Guid.NewGuid(), Name = "Triceps" };
        var back = new Muscle { Id = Guid.NewGuid(), Name = "Back" };

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Full Body Workout",
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var bench = new Exercise { Id = Guid.NewGuid(), Name = "Bench Press", Mechanic = "compound", PrimaryMuscleId = chest.Id, ExerciseType = ExerciseType.WeightReps };
        var overheadPress = new Exercise { Id = Guid.NewGuid(), Name = "Overhead Press", Mechanic = "compound", PrimaryMuscleId = shoulders.Id, ExerciseType = ExerciseType.WeightReps };
        var dips = new Exercise { Id = Guid.NewGuid(), Name = "Dips", Mechanic = "compound", PrimaryMuscleId = triceps.Id, ExerciseType = ExerciseType.WeightReps };
        var row = new Exercise { Id = Guid.NewGuid(), Name = "Barbell Row", Mechanic = "compound", PrimaryMuscleId = back.Id, ExerciseType = ExerciseType.WeightReps };

        context.Users.Add(user);
        context.Muscles.AddRange(chest, shoulders, triceps, back);
        context.Workouts.Add(workout);
        context.Exercises.AddRange(bench, overheadPress, dips, row);
        await context.SaveChangesAsync();

        context.WorkoutExercises.AddRange(
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = bench.Id, OrderIndex = 0 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = overheadPress.Id, OrderIndex = 1 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = dips.Id, OrderIndex = 2 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workout.Id, ExerciseId = row.Id, OrderIndex = 3 }
        );
        await context.SaveChangesAsync();

        var handler = new GetWorkoutDetailHandler(context);

        // 15 Minutes
        var result15 = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 15), CancellationToken.None);
        result15.Should().NotBeNull();
        result15.Exercises.Length.Should().Be(2);
        foreach (var ex in result15.Exercises)
        {
            ex.Sets.Length.Should().Be(2);
        }

        // 30 Minutes
        var result30 = await handler.Handle(new GetWorkoutDetailQuery(workout.Id, user.Id, true, 30), CancellationToken.None);
        result30.Should().NotBeNull();
        result30.Exercises.Length.Should().Be(3);
        foreach (var ex in result30.Exercises)
        {
            ex.Sets.Length.Should().Be(3);
        }
    }

}