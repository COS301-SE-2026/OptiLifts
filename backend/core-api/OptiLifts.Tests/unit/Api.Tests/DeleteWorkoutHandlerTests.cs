
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.DeleteWorkout;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Api.Tests;

public class DeleteWorkoutHandlertests
{
    [Fact]
    public async Task Handle_ReturnsFalse_WhenWorkoutDoesNotExist()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var handler = new DeleteWorkoutHandler(db);
        var result = await handler.Handle(
            new DeleteWorkoutCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);
        
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeletesWorkoutAndCascades_WhenOwnedWorkoutExists()
    {
        var userId = Guid.NewGuid();
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;


        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "x",
            DisplayName = "Test guy"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Leg Day",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        var muscle = new Muscle
        {
            Name = "Quadriceps"
        };
        db.Muscles.Add(muscle);
        await db.SaveChangesAsync();

        var exercise = new Exercise
        {
            Name = "Back Squat",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var workoutExer = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            OrderIndex = 0
        };
        db.WorkoutExercises.Add(workoutExer);
        await db.SaveChangesAsync();
        var set = new WorkoutSet
        {
            WorkoutExerciseId = workoutExer.Id,
            OrderIndex = 0,
            Reps = 10,
            Weight = 100,
            RestTime = 120
        };
        db.Sets.Add(set);
        await db.SaveChangesAsync();

        var handler = new DeleteWorkoutHandler(db);

        var result = await handler.Handle(
            new DeleteWorkoutCommand(workout.Id, userId),
            CancellationToken.None);
        result.Should().BeTrue();

        //check cascading
        var deleted = await db.Workouts.FindAsync(workout.Id);
        deleted.Should().BeNull();

        var delExercises = await db.WorkoutExercises
            .Where(we => we.WorkoutId == workout.Id)
            .ToListAsync();
        delExercises.Should().BeEmpty();

        var deleSets = await db.Sets
            .Where(s => s.WorkoutExerciseId == workoutExer.Id)
            .ToListAsync();
        deleSets.Should().BeEmpty();
    }
}