using System.IdentityModel.Tokens.Jwt;
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
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var query = new GetExercisesQuery(userId);
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

        var exerciseId = await _mediator.Send(command, cancellationToken);
        return Ok(new { Id = exerciseId });
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
