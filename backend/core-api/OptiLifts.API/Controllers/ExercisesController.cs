using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Exercises.CreateCustomExercise;
using OptiLifts.Application.Exercises.DeleteCustomExercise;
using OptiLifts.Application.Exercises.GetExerciseImages;
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
    public async Task<IActionResult> GetExercises(
        [FromQuery] string? search,
        [FromQuery] string? muscle,
        [FromQuery] string? equipment,
        CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var query = new GetExercisesQuery(userId, search, muscle, equipment);
        var exercises = await _mediator.Send(query, cancellationToken);
        return Ok(exercises);
    }

    [HttpPost("custom")]
    public async Task<IActionResult> CreateCustomExercise([FromForm] CreateCustomExerciseRequest request, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        Stream? imageStream = null;
        string? imageFileName = null;
        string? imageContentType = null;

        if (request.Image != null)
        {
            imageStream = request.Image.OpenReadStream();
            imageFileName = request.Image.FileName;
            imageContentType = request.Image.ContentType;
        }

        var command = new CreateCustomExerciseCommand(
            userId,
            request.Name,
            request.Mechanic,
            request.Equipment,
            request.Category,
            request.PrimaryMuscles,
            request.SecondaryMuscles,
            imageStream,
            imageFileName,
            imageContentType);

        try
        {
            var exerciseId = await _mediator.Send(command, cancellationToken);
            return Ok(new { Id = exerciseId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred while creating the exercise.", details = ex.Message });
        }
    }

    [HttpDelete("custom/{exerciseId:guid}")]
    public async Task<IActionResult> DeleteCustomExercise(Guid exerciseId, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        try
        {
            var deleted = await _mediator.Send(new DeleteCustomExerciseCommand(exerciseId, userId), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("images")]
    public async Task<ActionResult<Dictionary<string, string>>> GetExerciseImages([FromBody] GetExerciseImagesRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExerciseImagesQuery(request.Exercises), cancellationToken);
        return Ok(result);
    }
}

public class CreateCustomExerciseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Mechanic { get; set; }
    public string? Equipment { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> PrimaryMuscles { get; set; } = new();
    public List<string> SecondaryMuscles { get; set; } = new();
    public IFormFile? Image { get; set; }
}

public class GetExerciseImagesRequest
{
    public List<string> Exercises { get; set; } = new();
}
