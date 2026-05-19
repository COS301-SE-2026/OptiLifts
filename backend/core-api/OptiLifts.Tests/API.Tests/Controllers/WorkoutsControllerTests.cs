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

}