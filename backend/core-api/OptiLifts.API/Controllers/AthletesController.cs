using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Clash.Athletes.Commands;
using OptiLifts.Application.Clash.Athletes.Queries;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/clash/athletes")]
[Authorize]
public sealed class AthletesController : ControllerBase
{
    private readonly ISender _sender;

    public AthletesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{id:guid}/kudos")]
    public async Task<ActionResult<SendProfileKudosResult>> SendKudos(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new SendProfileKudosCommand(userId, id), cancellationToken);

        if (!res.Success)
        {
            return BadRequest(res);
        }

        return Ok(res);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

    [HttpGet("{id:guid}/profile")]
    public async Task<ActionResult<AthleteProfileResult>> GetProfile([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var profile = await _sender.Send(new GetAthleteProfileQuery(id, userId), cancellationToken);

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }
}

