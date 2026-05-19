using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Workouts;
using OptiLifts.Application.Workouts.GetWorkouts;

namespace OptiLifts.Tests.Api.Tests;

//test 1: no workouts = empty result
//test 2: 1 exercise = correct data
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
        var exercise = new Exercise
        {
            Name = "Bench",
            PrimaryMuscles = new List<string> { "Chest" },
            Category = "Strength"
        };

        db.Workouts.Add(workout);
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        workout = await db.Workouts.FirstAsync(w => w.CreatedBy == userId && w.Name == "A");
        exercise = await db.Exercises.FirstAsync(e => e.Name == "Bench");

        var set = new WorkoutSet
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
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
}
