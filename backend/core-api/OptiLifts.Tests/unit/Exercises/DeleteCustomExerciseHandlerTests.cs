using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.DeleteCustomExercise;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Exercises.DeleteCustomExercise;

namespace OptiLifts.Tests.Unit.Exercises;

public class DeleteCustomExerciseHandlerTests
{
    private static OptiLiftsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>().UseSqlite(connection).Options;

        var ctx = new OptiLiftsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Handle_SoftDeletion()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "delete-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Delete Test"
        });

        var muscle = new Muscle { Name = "Chest" };
        context.Muscles.Add(muscle);
        await context.SaveChangesAsync();

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Custom Press",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id,
            UserId = userId,
            ImageUrl = "https://example.blob.core.windows.net/exercises/abc.jpg"
        };

        context.Exercises.Add(exercise);
        context.SecMuscles.Add(new SecMuscle { ExerciseId = exercise.Id, MuscleId = muscle.Id });
        await context.SaveChangesAsync();

        var handler = new DeleteCustomExerciseHandler(context);

        var result = await handler.Handle(new DeleteCustomExerciseCommand(exercise.Id, userId), CancellationToken.None);

        result.Should().BeTrue();

        var store = await context.Exercises.FindAsync(exercise.Id);
        store.Should().NotBeNull();
        store!.IsDeleted.Should().BeTrue();
        store.ImageUrl.Should().Be(exercise.ImageUrl);

        (await context.SecMuscles.Where(s => s.ExerciseId == exercise.Id).ToListAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SoftDeletionWhenWorkoutIsInUse()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "delete-test-2@example.com",
            PasswordHash = "hash",
            DisplayName = "Delete Test 2"
        });

        var muscle = new Muscle { Name = "Back" };
        context.Muscles.Add(muscle);
        await context.SaveChangesAsync();

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Custom Row",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id,
            UserId = userId
        };

        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();

        var workout = new Workout
        {
            Name = "Used Workout",
            CreatedBy = userId
        };
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        context.WorkoutExercises.Add(new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            OrderIndex = 0
        });
        await context.SaveChangesAsync();

        var handler = new DeleteCustomExerciseHandler(context);

        var result = await handler.Handle(new DeleteCustomExerciseCommand(exercise.Id, userId), CancellationToken.None);

        result.Should().BeTrue();
        (await context.Exercises.FindAsync(exercise.Id))!.IsDeleted.Should().BeTrue();
    }
}