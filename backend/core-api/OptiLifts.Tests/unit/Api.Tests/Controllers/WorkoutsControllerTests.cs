using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Workouts.AddExerciseToWorkout;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Api.Tests.Controllers;

public class WorkoutsControllerTests
{
    [Fact]
    public async Task AddExerciseToWorkout_ShouldReturnNoContent_WhenSenderAddsExercise()
    {
        var userId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var context = CreateSqliteDbContext(connection);
        SeedWorkoutAndExercise(context, userId, workoutId, exerciseId);

        var handler = new AddExerciseToWorkoutHandler(context);
        var added = await handler.Handle(new AddExerciseToWorkoutCommand(workoutId, userId, exerciseId), CancellationToken.None);

        added.Should().BeTrue();
    }

    [Fact]
    public async Task AddExerciseToWorkout_ShouldReturnUnauthorized_WhenSubClaimIsMissing()
    {
        var sender = new Mock<ISender>();
        var controller = new WorkoutsController(sender.Object)
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var result = await controller.AddExerciseToWorkout(
            Guid.NewGuid(),
            new WorkoutsController.AddExerciseToWorkoutRequest(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddExerciseToWorkout_ShouldReturnNotFound_WhenSenderRejectsRequest()
    {
        var userId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var context = CreateSqliteDbContext(connection);
        SeedUserAndWorkoutOnly(context, userId, workoutId);

        var handler = new AddExerciseToWorkoutHandler(context);
        var added = await handler.Handle(new AddExerciseToWorkoutCommand(workoutId, userId, exerciseId), CancellationToken.None);

        added.Should().BeFalse();
    }

    private static OptiLiftsDbContext CreateSqliteDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new OptiLiftsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedWorkoutAndExercise(OptiLiftsDbContext context, Guid userId, Guid workoutId, Guid exerciseId)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        });

        var folderId = Guid.NewGuid();
        context.Folders.Add(new Folder
        {
            Id = folderId,
            UserId = userId,
            Name = "Default"
        });

        context.Workouts.Add(new Workout
        {
            Id = workoutId,
            CreatedBy = userId,
            FolderId = folderId,
            Name = "Push Day"
        });

        context.Exercises.Add(new Exercise
        {
            Id = exerciseId,
            Name = "Bench Press",
            Category = "Strength",
            PrimaryMuscles = new List<string> { "Chest" }
        });

        context.SaveChanges();
    }

    private static void SeedUserAndWorkoutOnly(OptiLiftsDbContext context, Guid userId, Guid workoutId)
    {
        context.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        });

        var folderId = Guid.NewGuid();
        context.Folders.Add(new Folder
        {
            Id = folderId,
            UserId = userId,
            Name = "Default"
        });

        context.Workouts.Add(new Workout
        {
            Id = workoutId,
            CreatedBy = userId,
            FolderId = folderId,
            Name = "Push Day"
        });

        context.SaveChanges();
    }
}
