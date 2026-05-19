using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Exercises.CreateCustomExercise;
using OptiLifts.Application.Exercises.GetExercises;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExercisesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExercisesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetExercises(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var query = new GetExercisesQuery(userId);
        var exercises = await _mediator.Send(query, cancellationToken);
        return Ok(exercises);
    }

    [HttpPost("custom")]
    public async Task<IActionResult> CreateCustomExercise([FromBody] CreateCustomExerciseRequest request, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var command = new CreateCustomExerciseCommand(
            userId,
            request.Name,
            request.Mechanic,
            request.Equipment,
            request.Category,
            request.PrimaryMuscles,
            request.SecondaryMuscles);

        var exerciseId = await _mediator.Send(command, cancellationToken);
        return Ok(new { Id = exerciseId });
    }
}

public record CreateCustomExerciseRequest(
    string Name,
    string? Mechanic,
    string? Equipment,
    string Category,
    List<string> PrimaryMuscles,
    List<string> SecondaryMuscles);
