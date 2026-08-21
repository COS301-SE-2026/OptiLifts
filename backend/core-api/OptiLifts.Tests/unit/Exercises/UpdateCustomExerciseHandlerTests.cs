using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Exercises.UpdateCustomExercise;
using OptiLifts.Application.Storage;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Exercises.UpdateCustomExercise;

namespace OptiLifts.Tests.Unit.Exercises;

public class UpdateCustomExerciseHandlerTests
{
    private static OptiLiftsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>().UseSqlite(connection).Options;
        var ctx = new OptiLiftsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperationException_WhenNameIsDuplicateOfPublicExercise()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "update-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Update Test"
        });

        var muscle = new Muscle { Name = "Chest" };
        context.Muscles.Add(muscle);

        context.Exercises.Add(new Exercise
        {
            Name = "Incline Bench Press",
            UserId = null,
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
            IsDeleted = false
        });

        var customExercise = new Exercise
        {
            Name = "My Press",
            UserId = userId,
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
            IsDeleted = false
        };
        context.Exercises.Add(customExercise);
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var handler = new UpdateCustomExerciseHandler(context, blobMock.Object);

        var act = () => handler.Handle(
            new UpdateCustomExerciseCommand(
                customExercise.Id,
                userId,
                "incline bench press",
                null,
                null,
                null,
                false),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_AllowsUpdating_WhenNameIsUnchanged()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "update-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Update Test"
        });

        var muscle = new Muscle { Name = "Chest" };
        context.Muscles.Add(muscle);

        var customExercise = new Exercise
        {
            Name = "My Press",
            UserId = userId,
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
            IsDeleted = false
        };
        context.Exercises.Add(customExercise);
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var handler = new UpdateCustomExerciseHandler(context, blobMock.Object);

        var result = await handler.Handle(
            new UpdateCustomExerciseCommand(
                customExercise.Id,
                userId,
                "My Press",
                null,
                null,
                null,
                false),
            CancellationToken.None);

        result.Should().BeTrue();
    }
}
