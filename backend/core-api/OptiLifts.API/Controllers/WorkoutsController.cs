using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Workouts.AddExerciseToWorkout;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Application.Workouts.DeleteWorkout;
using OptiLifts.Application.Workouts.DuplicateWorkout;

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

    [HttpPost]
    public async Task<ActionResult<CreateWorkoutResult>> CreateWorkout(
        [FromBody] CreateWorkoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var exercises = request.Exercises
            .Select(e => new CreateWorkoutExerciseDto(
                e.ExerciseId,
                e.OrderIndex,
                e.Sets.Select(s => new CreateWorkoutSetDto(
                    s.Type, s.Reps, s.Weight, s.Duration, s.Distance, s.OrderIndex, s.RestTime)).ToList()))
            .ToList();

        var command = new CreateWorkoutCommand(request.FolderId, request.Name, request.DayIndex, userId, exercises);
        var result = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetWorkouts), new { id = result.WorkoutId }, result);
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

    [HttpDelete("{workoutId:guid}")]
    public async Task<IActionResult> DeleteWorkout(
        [FromRoute] Guid workoutId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var deleted = await _sender.Send(new DeleteWorkoutCommand(workoutId, userId), cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                status = 404,
                title = "Not Found",
                message = "Workout was not found for this user."
            });
            
        }
        return Ok(new { message = "Workout deleted successfully." });

    }

    [HttpPost("{workoutId:guid}/duplicate")]
    public async Task<ActionResult<DuplicateWorkoutResult>> DuplicateWorkout(
        [FromRoute] Guid workoutId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var result = await _sender.Send(new DuplicateWorkoutCommand (workoutId, userId), cancellationToken);

        if (result == null)
        {
            return NotFound(new
            {
                status = 404,
                title = "Not Found",
                message = "Source workout was not found for this user."
            });
        }
        return CreatedAtAction(nameof(GetWorkouts), new {id = result.WorkoutId }, result);

    }
    
    
}
