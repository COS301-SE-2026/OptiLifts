using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Training.GetPlateauPage;
using OptiLifts.Application.Training.RecordAcuteFatigue;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TrainingController : ControllerBase
{
    private readonly ISender _sender;

    public TrainingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("plateau-page")]
    public async Task<ActionResult<IReadOnlyList<ExerciseDiagnosisDto>>> GetPlateauPage(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new GetPlateauPageQuery(userId), cancellationToken);
        return Ok(res);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdVal = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdVal, out userId);
    }

    public sealed record RecordAcuteFatigueRequest(string MuscleGroup);

    [HttpPost("acute-fatigue")]
    public async Task<IActionResult> RecordAcuteFatigue([FromBody] RecordAcuteFatigueRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _sender.Send(new RecordAcuteFatigueCommand(userId, request.MuscleGroup), cancellationToken);
        return Ok();
    }
}
