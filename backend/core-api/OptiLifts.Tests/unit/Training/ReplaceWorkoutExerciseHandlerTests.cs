using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.ReplaceWorkoutExercise;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Unit.Workouts;

public class ReplaceWorkoutExerciseHandlerTests
{
    private static async Task<OptiLiftsDbContext> NewDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var db = new OptiLiftsDbContext(new DbContextOptionsBuilder<OptiLiftsDbContext>().UseSqlite(conn).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task<(Guid UserId, Guid WorkoutId, Guid OldExerciseId, Guid NewExerciseId)> SeedAsync(OptiLiftsDbContext db)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@x.com", PasswordHash = "x", DisplayName = "U" };
        var muscle = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };
        var oldExer = new Exercise { Id = Guid.NewGuid(), Name = "Old", Mechanic = "compound", Equipment = "barbell", PrimaryMuscleId = muscle.Id, ExerciseType = ExerciseType.WeightReps };
        var newExer = new Exercise { Id = Guid.NewGuid(), Name = "New", Mechanic = "compound", Equipment = "barbell", PrimaryMuscleId = muscle.Id, ExerciseType = ExerciseType.WeightReps };
        var workout = new Workout { Id = Guid.NewGuid(), Name = "W", CreatedBy = user.Id };
        db.AddRange(user, muscle, oldExer, newExer, workout);
        await db.SaveChangesAsync();
        return (user.Id, workout.Id, oldExer.Id, newExer.Id);
    }

    [Fact]
    public async Task Handle_ReturnsFalseWorkoutNotOwnedByUser()
    {
        var db = await NewDbAsync();
        var (_, workoutId, oldId, newId) = await SeedAsync(db);

        var handler = new ReplaceWorkoutExerciseHandler(db);
        var res = await handler.Handle(new ReplaceWorkoutExerciseCommand(Guid.NewGuid(), workoutId, oldId, newId), CancellationToken.None);

        res.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFalseNewExerciseDoesNotExist()
    {
        var db = await NewDbAsync();
        var (userId, workoutId, oldId, _) = await SeedAsync(db);
        db.WorkoutExercises.Add(new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workoutId, ExerciseId = oldId, OrderIndex = 0 });
        await db.SaveChangesAsync();

        var handler = new ReplaceWorkoutExerciseHandler(db);
        var res = await handler.Handle(new ReplaceWorkoutExerciseCommand(userId, workoutId, oldId, Guid.NewGuid()), CancellationToken.None);

        res.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFalseOldExerNotInWorkout()
    {
        var db = await NewDbAsync();
        var (userId, workoutId, oldId, newId) = await SeedAsync(db);

        var handler = new ReplaceWorkoutExerciseHandler(db);
        var res = await handler.Handle(new ReplaceWorkoutExerciseCommand(userId, workoutId, oldId, newId), CancellationToken.None);

        res.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReplacesOnlyMatchingRowsInReqWorkoutLeavesOtherWorkouts()
    {
        var db = await NewDbAsync();
        var (userId, workoutId, oldId, newId) = await SeedAsync(db);

        var otherWorkout = new Workout { Id = Guid.NewGuid(), Name = "Other", CreatedBy = userId };
        db.Workouts.Add(otherWorkout);
        await db.SaveChangesAsync();

        db.WorkoutExercises.AddRange(
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workoutId, ExerciseId = oldId, OrderIndex = 0 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = workoutId, ExerciseId = oldId, OrderIndex = 1 },
            new WorkoutExercise { Id = Guid.NewGuid(), WorkoutId = otherWorkout.Id, ExerciseId = oldId, OrderIndex = 0 });
        await db.SaveChangesAsync();

        var handler = new ReplaceWorkoutExerciseHandler(db);
        var res = await handler.Handle(new ReplaceWorkoutExerciseCommand(userId, workoutId, oldId, newId), CancellationToken.None);

        res.Should().BeTrue();

        var targRows = await db.WorkoutExercises.Where(we => we.WorkoutId == workoutId).ToListAsync();
        targRows.Should().OnlyContain(we => we.ExerciseId == newId);

        var otherRows = await db.WorkoutExercises.Where(we => we.WorkoutId == otherWorkout.Id).ToListAsync();
        otherRows.Should().OnlyContain(we => we.ExerciseId == oldId);
    }
}
