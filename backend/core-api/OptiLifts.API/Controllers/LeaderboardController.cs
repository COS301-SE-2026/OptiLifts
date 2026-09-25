using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Application.Clash.Leaderboard.Queries;

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
        var userIdVal = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdVal, out userId);
    }

    [HttpGet("global")]
    public async Task<ActionResult<LeaderboardPageResult>> GetGlobal(
        [FromQuery] string metric = "DotsOverall",
        [FromQuery] string timeframe = "monthly",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new GetGlobalLeaderboardQuery(userId, metric, timeframe, page, pageSize), cancellationToken);
        return Ok(res);
    }

    [HttpGet("divisional")]
    public async Task<ActionResult<LeaderboardPageResult>> GetDivisional(
        [FromQuery] string gender,
        [FromQuery] string bracketId,
        [FromQuery] string metric = "DotsOverall",
        [FromQuery] string timeframe = "monthly",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new GetDivisionalLeaderboardQuery(userId, gender, bracketId, metric, timeframe, page, pageSize), cancellationToken);
        return Ok(res);
    }

}
