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
}