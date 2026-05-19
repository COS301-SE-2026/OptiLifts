using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Exercises.GetExercises;
using OptiLifts.Application.Exercises.CreateCustomExercise;
using Xunit;

namespace OptiLifts.Tests.Application.Tests;

public class ExercisesControllerTests
{
    [Fact]
    public async Task GetExercises_ReturnsOkWithExercises_WhenUserIsAuthorized()
    {
        var userId = Guid.NewGuid();
        var exercises = new List<ExerciseDto>
        {
            new ExerciseDto(Guid.NewGuid(), "Bench Press", "Compound", "Barbell", "Strength", new List<string>{"Chest"}, new List<string>{"Triceps"}, false)
        };

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetExercisesQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercises);

        var controller = new ExercisesController(mediatorMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await controller.GetExercises(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(exercises);
    }

    [Fact]
    public async Task CreateCustomExercise_ReturnsOkWithId_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var createdId = Guid.NewGuid();

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateCustomExerciseCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdId);

        var controller = new ExercisesController(mediatorMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var request = new CreateCustomExerciseRequest(
            "Custom Move",
            "Isolation",
            "Dumbbell",
            "Accessory",
            new List<string>{"Biceps"},
            new List<string>{"Forearms"}
        );

        var result = await controller.CreateCustomExercise(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(new { Id = createdId });
    }
}
