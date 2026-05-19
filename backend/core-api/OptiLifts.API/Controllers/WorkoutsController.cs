using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using OptiLifts.Application.Workouts.AddExerciseToWorkout;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Application.Workouts.CreateWorkout;

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
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var sets = request.Sets
            .Select(s => new CreateWorkoutSetDto(s.ExerciseId, s.Type, s.Reps, s.Weight, s.OrderIndex, s.RestTime))
            .ToList();

        var command = new CreateWorkoutCommand(request.FolderId, request.Name, request.DayIndex, userId, sets);
        var result = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetWorkouts), new { id = result.WorkoutId }, result);
        
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
