using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Api.Tests;

//test 1: no workouts = empty result
//test 2: 1 exercise = correct data
//test 3: 2 exercises 
//future additions: filtering, sorting, multiple workouts etc

public class GetWorkoutsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoWorkouts()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        db.Database.EnsureCreated();

        var handler = new GetWorkoutsHandler(db);
        var result = await handler.Handle(new GetWorkoutsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsWorkoutCard_ForExistingWorkout()
    {
        var userId = Guid.NewGuid();

        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        db.Database.EnsureCreated();

        var user = new OptiLifts.Domain.Users.User
        {
            Id = userId,
            Email = "u@example.com",
            PasswordHash = "x",
            DisplayName = "u"
        };
        db.Users.Add(user);

        var folder = new Folder
        {
            Name = "Default",
            UserId = userId
        };
        db.Folders.Add(folder);
        await db.SaveChangesAsync();

        //check persistence
        folder = await db.Folders.FirstAsync(f => f.UserId == userId && f.Name == "Default");

        var workout = new Workout
        {
            Name = "A",
            CreatedBy = userId,
            FolderId = folder.Id
        };
        var chest = new Muscle
        {
            Name = "Chest"
        };
        db.Muscles.Add(chest);
        await db.SaveChangesAsync();
        var exercise = new Exercise
        {
            Name = "Bench",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = chest.Id,
            UserId = null,
            ImageUrl = null
        };

        db.Workouts.Add(workout);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        workout = await db.Workouts.FirstAsync(w => w.CreatedBy == userId && w.Name == "A");
        exercise = await db.Exercises.FirstAsync(e => e.Name == "Bench");

        var workoutExercise = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            OrderIndex = 0
        };

        db.WorkoutExercises.Add(workoutExercise);
        var set = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            OrderIndex = 0,
            Reps = 1,
            Weight = 10,
            RestTime = 60
        };
        db.Sets.Add(set);

        await db.SaveChangesAsync();

        var handler = new GetWorkoutsHandler(db);
        var result = await handler.Handle(new GetWorkoutsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        var card = result[0];
        card.Id.Should().Be(workout.Id);
        card.Name.Should().Be("A");
        card.ExerciseCount.Should().Be(1);
        card.ExercisePreview.Should().Contain("Bench");
        card.PrimaryMuscleGroups.Should().Contain("Chest");
    }

    [Fact]
    public async Task Handle_ReturnsWorkoutCard_ForTwoExercises()
    {
        var userId = Guid.NewGuid();

        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        db.Database.EnsureCreated();

        var user = new OptiLifts.Domain.Users.User
        {
            Id = userId,
            Email = "u2@example.com",
            PasswordHash = "x",
            DisplayName = "u2"
        };
        db.Users.Add(user);

        var folder = new Folder
        {
            Name = "Default",
            UserId = userId
        };
        db.Folders.Add(folder);
        await db.SaveChangesAsync();

        folder = await db.Folders.FirstAsync(f => f.UserId == userId && f.Name == "Default");
        //new db changes require muscles
        var chest = new Muscle
        {
            Name = "Chest"
        };
        var legs = new Muscle
        {
            Name = "Legs"
        };
        db.Muscles.AddRange(chest, legs);
        await db.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "B",
            CreatedBy = userId,
            FolderId = folder.Id
        };

        var exercise1 = new Exercise
        {
            Name = "Bench",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = chest.Id,
            UserId = null,
            ImageUrl = null
        };

        var exercise2 = new Exercise
        {
            Name = "Squat",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = legs.Id,
            UserId = null,
            ImageUrl = null
        };

        db.Workouts.Add(workout);
        db.Exercises.AddRange(exercise1, exercise2);
        await db.SaveChangesAsync();

        workout = await db.Workouts.FirstAsync(w => w.CreatedBy == userId && w.Name == "B");
        exercise1 = await db.Exercises.FirstAsync(e => e.Name == "Bench");
        exercise2 = await db.Exercises.FirstAsync(e => e.Name == "Squat");

        //new db changes
        var workoutExercise1 = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise1.Id,
            OrderIndex = 0
        };
        var workoutExercise2 = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise2.Id,
            OrderIndex = 1
        };
        db.WorkoutExercises.AddRange(workoutExercise1, workoutExercise2);
        await db.SaveChangesAsync();

        var set1 = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise1.Id,
            OrderIndex = 0,
            Reps = 5,
            Weight = 100,
            RestTime = 90
        };

        var set2 = new WorkoutSet
        {
            WorkoutExerciseId = workoutExercise2.Id,
            OrderIndex = 1,
            Reps = 5,
            Weight = 150,
            RestTime = 120
        };

        db.Sets.AddRange(set1, set2);
        await db.SaveChangesAsync();

        var handler = new GetWorkoutsHandler(db);
        var result = await handler.Handle(new GetWorkoutsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        var card = result[0];
        card.Id.Should().Be(workout.Id);
        card.Name.Should().Be("B");
        card.ExerciseCount.Should().Be(2);
        card.ExercisePreview.Should().Contain("Bench");
        card.ExercisePreview.Should().Contain("Squat");
        card.PrimaryMuscleGroups.Should().Contain("Chest");
        card.PrimaryMuscleGroups.Should().Contain("Legs");
    }

}
