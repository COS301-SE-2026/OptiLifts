using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Clash.Leaderboard.Commands;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/clash/leaderboard")]
[Authorize]
public sealed class LeaderboardController : ControllerBase
{
    private readonly ISender _sender;

    public LeaderboardController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record ToggleOptInApiRequest(bool OptIn);

    [HttpPost("opt-in")]
    public async Task<ActionResult<ToggleLeaderboardOptInResult>> ToggleOptIn(
        [FromBody] ToggleOptInApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new ToggleLeaderboardOptInCommand(userId, request.OptIn), cancellationToken);
        
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
}
