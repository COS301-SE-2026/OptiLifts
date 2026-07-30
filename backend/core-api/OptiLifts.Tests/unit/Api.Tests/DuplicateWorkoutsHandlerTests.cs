using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.DuplicateWorkout;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Api.Tests;

public class DuplicateWorkoutsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNull_WhenSourceWorkoutDoesNotExist()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var handler = new DuplicateWorkoutHandler(db);

        var result = await handler.Handle(
            new DuplicateWorkoutCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DupeWorkoutAndData_WhenOwnedWorkoutExists()
    {
        var userid = Guid.NewGuid();
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = userid,
            Email = "test@example.com",
            PasswordHash = "x",
            DisplayName = "Test person"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var folder = new Folder
        {
            Name = "My stuff",
            UserId = userid
        };
        db.Folders.Add(folder);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Leg Day",
            CreatedBy = userid,
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

        var handler = new DuplicateWorkoutHandler(db);

        var result = await handler.Handle(
            new DuplicateWorkoutCommand(workout.Id, userid),
            CancellationToken.None);
        result.Should().NotBeNull();
        result!.WorkoutId.Should().NotBe(workout.Id);
        result.Name.Should().Be("Leg Day");
        result.FolderId.Should().Be(folder.Id);

        var dupe = await db.Workouts.FindAsync(result.WorkoutId);
        dupe.Should().NotBeNull();
        dupe.CreatedBy.Should().Be(userid);
        var dupeExercises = await db.WorkoutExercises
            .Where(we => we.WorkoutId == result.WorkoutId)
            .ToListAsync();
        dupeExercises.Should().HaveCount(1);

        var dupeExercise = dupeExercises[0];
        dupeExercise.Id.Should().NotBe(workoutExer.Id);
        dupeExercise.ExerciseId.Should().Be(exercise.Id);
        dupeExercise.OrderIndex.Should().Be(1);

        var dupeSets = await db.Sets
            .Where(s => s.WorkoutExerciseId == dupeExercise.Id)
            .ToListAsync();
        dupeSets.Should().HaveCount(1);

        var dupeSet = dupeSets[0];
        dupeSet.Id.Should().NotBe(set.Id);
        dupeSet.OrderIndex.Should().Be(0);
        dupeSet.Reps.Should().Be(10);
        dupeSet.Weight.Should().Be(100);
        dupeSet.RestTime.Should().Be(120);

    }

}