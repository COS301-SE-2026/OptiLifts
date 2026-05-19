// EXAMPLE FILE - shows how to document API endpoints for Swagger
// Copy this pattern when adding real controllers

using Microsoft.AspNetCore.Mvc;

namespace OptiLifts.API.Controllers;

//def the shape of request/response JSON bodies here (DTO)

public record WorkoutResponse(
    string Id,
    string Name,
    string[] PrimaryMuscleGroups,
    int ExerciseCount
);

public record CreateWorkoutRequest(
    string Name,
    string[] PrimaryMuscleGroups
);

public record ErrorResponse(string Message);

// --- Controller ---

/// <summary>Manages workout plans.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkoutsController : ControllerBase
{
    /// <summary>Get all workouts for the current user.</summary>
    /// <returns>A list of workouts.</returns>
    /// <response code="200">Returns the list of workouts.</response>
    /// <response code="401">Unauthorized - missing or invalid token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetAll()
    {
        //wire up MediatR query
        var workouts = new[]
        {
            new WorkoutResponse("w1", "Push Day A", ["Chest", "Triceps"], 5),
            new WorkoutResponse("w2", "Pull Day B", ["Lats", "Biceps"],  4),
        };
        return Ok(workouts);
    }

    /// <summary>Get a single workout by ID.</summary>
    /// <param name="id">The workout ID.</param>
    /// <returns>The matching workout.</returns>
    /// <response code="200">Returns the workout.</response>
    /// <response code="404">Workout not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        //wire up MediatR query
        return NotFound(new ErrorResponse($"Workout '{id}' not found."));
    }

    /// <summary>Create a new workout.</summary>
    /// <param name="request">The workout details.</param>
    /// <returns>The newly created workout.</returns>
    /// <response code="201">Workout created successfully.</response>
    /// <response code="400">Invalid request body.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WorkoutResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateWorkoutRequest request)
    {
        //wire up MediatR command
        var created = new WorkoutResponse(
            Guid.NewGuid().ToString(),
            request.Name,
            request.PrimaryMuscleGroups,
            0
        );
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Delete a workout by ID.</summary>
    /// <param name="id">The workout ID to delete.</param>
    /// <response code="204">Deleted successfully.</response>
    /// <response code="404">Workout not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        //wire up MediatR command
        return NoContent();
    }
}
