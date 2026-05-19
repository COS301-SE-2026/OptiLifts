using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using OptiLifts.Application.Workouts.AddExerciseToWorkout;
using OptiLifts.Application.Workouts.GetWorkouts;

namespace OptiLifts.API.Controllers;

//http entrypoint for workout related api calls
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WorkoutsController : ControllerBase
{
    private readonly ISender _sender;

    public sealed record AddExerciseToWorkoutRequest(Guid ExerciseId);

    public WorkoutsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkoutCardDto>>> GetWorkouts(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(new GetWorkoutsQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{workoutId:guid}/exercises")]
    public async Task<IActionResult> AddExerciseToWorkout(
        [FromRoute] Guid workoutId,
        [FromBody] AddExerciseToWorkoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var added = await _sender.Send(
            new AddExerciseToWorkoutCommand(workoutId, userId, request.ExerciseId),
            cancellationToken);

        return added ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }
}
