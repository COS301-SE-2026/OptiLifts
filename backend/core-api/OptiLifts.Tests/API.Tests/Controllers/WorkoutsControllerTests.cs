using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Workouts.AddExerciseToWorkout;

namespace OptiLifts.Tests.API.Tests.Controllers;

public class WorkoutsControllerTests
{
    [Fact]
    public async Task AddExerciseToWorkout_ShouldReturnNoContent_WhenSenderAddsExercise()
    {
        var userId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<AddExerciseToWorkoutCommand>(command =>
                    command.UserId == userId &&
                    command.WorkoutId == workoutId &&
                    command.ExerciseId == exerciseId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController(sender.Object, userId);

        var result = await controller.AddExerciseToWorkout(
            workoutId,
            new WorkoutsController.AddExerciseToWorkoutRequest(exerciseId),
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task AddExerciseToWorkout_ShouldReturnUnauthorized_WhenSubClaimIsMissing()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender.Object, null);

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
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<AddExerciseToWorkoutCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController(sender.Object, userId);

        var result = await controller.AddExerciseToWorkout(
            Guid.NewGuid(),
            new WorkoutsController.AddExerciseToWorkoutRequest(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }
}