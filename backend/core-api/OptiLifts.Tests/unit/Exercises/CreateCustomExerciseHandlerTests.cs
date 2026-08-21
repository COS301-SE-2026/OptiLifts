using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Exercises.CreateCustomExercise;
using OptiLifts.Application.Storage;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Exercises.CreateCustomExercise;

namespace OptiLifts.Tests.Unit.Exercises;

public class CreateCustomExerciseHandlerTests
{
    private static OptiLiftsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>().UseSqlite(connection).Options;

        var ctx = new OptiLiftsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Handle_UploadsImageToBlobStorage_AndPersistsReturnedUrl()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "create-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Create Test"
        });

        var muscle = new Muscle { Name = "Biceps" };
        context.Muscles.Add(muscle);
        await context.SaveChangesAsync();

        await using var imageStream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-bytes"));
        var blobMock = new Mock<IBlobStorageService>();
        blobMock
            .Setup(b => b.UploadFileAsync(
                imageStream,
                "curl.png",
                "image/png",
                "exercises",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.blob.core.windows.net/exercises/curl.png");

        var handler = new CreateCustomExerciseHandler(context, blobMock.Object);

        var exerciseId = await handler.Handle(
            new CreateCustomExerciseCommand(
                userId,
                "Custom Curl",
                "isolation",
                "dumbbell",
                "Strength",
                ["Biceps"],
                [],
                imageStream,
                "curl.png",
                "image/png"),
            CancellationToken.None);

        var exercise = await context.Exercises.SingleAsync(e => e.Id == exerciseId);

        exercise.ImageUrl.Should().Be("https://example.blob.core.windows.net/exercises/curl.png");
        blobMock.VerifyAll();
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
            Email = "create-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Create Test"
        });

        var muscle = new Muscle { Name = "Chest" };
        context.Muscles.Add(muscle);

        context.Exercises.Add(new Exercise
        {
            Name = "Bench Press",
            UserId = null,
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
            IsDeleted = false
        });
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var handler = new CreateCustomExerciseHandler(context, blobMock.Object);

        var act = () => handler.Handle(
            new CreateCustomExerciseCommand(
                userId,
                "bench press", // case-insensitive check
                "compound",
                "barbell",
                "Strength",
                ["Chest"],
                [],
                null,
                null,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        blobMock.Verify(b => b.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperationException_WhenNameIsDuplicateOfUserCustomExercise()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "create-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Create Test"
        });

        var muscle = new Muscle { Name = "Biceps" };
        context.Muscles.Add(muscle);

        context.Exercises.Add(new Exercise
        {
            Name = "My Special Curl",
            UserId = userId,
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
            IsDeleted = false
        });
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var handler = new CreateCustomExerciseHandler(context, blobMock.Object);

        var act = () => handler.Handle(
            new CreateCustomExerciseCommand(
                userId,
                "  my special curl  ", // whitespace and case-insensitive check
                "isolation",
                "dumbbell",
                "Strength",
                ["Biceps"],
                [],
                null,
                null,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_AllowsCreation_WhenNameMatchesCustomExerciseOfDifferentUser()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        context.Users.AddRange(
            new User { Id = user1, Email = "u1@test.com", EmailHash = "h1", PasswordHash = "h", DisplayName = "U1" },
            new User { Id = user2, Email = "u2@test.com", EmailHash = "h2", PasswordHash = "h", DisplayName = "U2" });

        var muscle = new Muscle { Name = "Back" };
        context.Muscles.Add(muscle);

        context.Exercises.Add(new Exercise
        {
            Name = "Unique Row",
            UserId = user2,
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
            IsDeleted = false
        });
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var handler = new CreateCustomExerciseHandler(context, blobMock.Object);

        var exerciseId = await handler.Handle(
            new CreateCustomExerciseCommand(
                user1,
                "Unique Row",
                "compound",
                "barbell",
                "Strength",
                ["Back"],
                [],
                null,
                null,
                null),
            CancellationToken.None);

        exerciseId.Should().NotBeEmpty();
        var created = await context.Exercises.SingleOrDefaultAsync(e => e.Id == exerciseId);
        created.Should().NotBeNull();
        created!.UserId.Should().Be(user1);
    }

    [Fact]
    public async Task Handle_AllowsCreation_WhenNameMatchesSoftDeletedExercise()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "create-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Create Test"
        });

        var muscle = new Muscle { Name = "Biceps" };
        context.Muscles.Add(muscle);

        context.Exercises.Add(new Exercise
        {
            Name = "Old Deleted Exercise",
            UserId = userId,
            PrimaryMuscleId = muscle.Id,
            ExerciseType = ExerciseType.WeightReps,
            IsDeleted = true
        });
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var handler = new CreateCustomExerciseHandler(context, blobMock.Object);

        var exerciseId = await handler.Handle(
            new CreateCustomExerciseCommand(
                userId,
                "Old Deleted Exercise",
                "isolation",
                "dumbbell",
                "Strength",
                ["Biceps"],
                [],
                null,
                null,
                null),
            CancellationToken.None);

        exerciseId.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_ThrowsInvalidOperationException_WhenNameIsEmptyOrWhitespace(string? name)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "create-test@example.com",
            PasswordHash = "hash",
            DisplayName = "Create Test"
        });

        var muscle = new Muscle { Name = "Biceps" };
        context.Muscles.Add(muscle);
        await context.SaveChangesAsync();

        var blobMock = new Mock<IBlobStorageService>();
        var handler = new CreateCustomExerciseHandler(context, blobMock.Object);

        var act = () => handler.Handle(
            new CreateCustomExerciseCommand(
                userId,
                name!,
                "isolation",
                "dumbbell",
                "Strength",
                ["Biceps"],
                [],
                null,
                null,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Exercise name is required.");
    }
}