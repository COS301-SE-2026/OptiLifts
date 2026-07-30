using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.UpdateWorkout;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Api.Tests;

public class UpdateWorkoutHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsFalse_WhenWOrkoutDoesNotExist()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var handler = new UpdateWorkoutHandler(db);

        var result = await handler.Handle(
            new UpdateWorkoutCommand(Guid.NewGuid(), Guid.NewGuid(), null, "NewName", [], []),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UpdateWorkout_WhenWorkoutExists()
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
            DisplayName = "Test person"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var folder = new Folder
        {
            Name = "My stuff",
            UserId = userId
        };
        db.Folders.Add(folder);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Leg Day",
            CreatedBy = userId,
            FolderId = folder.Id
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
            OrderIndex = 1
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

        var handler = new UpdateWorkoutHandler(db);
        var setsToSave = new List<UpdateWorkoutSetDto>
        {
            new UpdateWorkoutSetDto("W", 12, 40.0f, null, null , 0, 60),
            new UpdateWorkoutSetDto("I", 8, 80.0f, null, null , 1, 90)
        };
        var exercisesToSave = new List<UpdateWorkoutExerciseDto>
        {
            new UpdateWorkoutExerciseDto(exercise.Id, 0, setsToSave)
        };

        var command = new UpdateWorkoutCommand(
            workout.Id,
            user.Id,
            null,
            "Updated Name",
            exercisesToSave);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeTrue();
        var updated = await db.Workouts.FindAsync(workout.Id);
        updated!.Name.Should().Be("Updated Name");


    }
}