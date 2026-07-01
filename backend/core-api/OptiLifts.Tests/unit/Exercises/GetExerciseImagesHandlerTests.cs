using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.GetExerciseImages;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Exercises.GetExerciseImages;

namespace OptiLifts.Tests.Unit.Exercises;

public class GetExerciseImagesHandlerTests
{
    private static OptiLiftsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new OptiLiftsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyExercisesWithImages()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var muscle = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };
        context.Add(muscle);

        var ex1 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            ImageUrl = "http://127.0.0.1:10000/images/bench.jpg",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id
        };

        var ex2 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Squat",
            ImageUrl = "http://127.0.0.1:10000/images/squat.jpg",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id
        };

        var exNoImg = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Deadlift",
            ImageUrl = null, 
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id
        };

        context.Exercises.AddRange(ex1, ex2, exNoImg);
        await context.SaveChangesAsync();

        var handler = new GetExerciseImagesHandler(context);
        var query = new GetExerciseImagesQuery();
        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2); 
        result.Should().ContainKey("Bench Press").WhoseValue.Should().Be("http://127.0.0.1:10000/images/bench.jpg");
        result.Should().ContainKey("Squat").WhoseValue.Should().Be("http://127.0.0.1:10000/images/squat.jpg");
        result.Should().NotContainKey("Deadlift");
    }

    [Fact]
    public async Task Handle_WhenNoExercisesHaveImages_ShouldReturnEmptyDictionary()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var muscle = new Muscle { Id = Guid.NewGuid(), Name = "Chest" };
        context.Add(muscle);

        var exNoImg = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Overhead Press",
            ImageUrl = null,
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscle.Id
        };

        context.Exercises.Add(exNoImg);
        await context.SaveChangesAsync();

        var handler = new GetExerciseImagesHandler(context);
        var query = new GetExerciseImagesQuery();
        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}