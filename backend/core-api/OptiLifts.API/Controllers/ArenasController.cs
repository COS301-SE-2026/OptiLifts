using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Arenas.Queries;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/v1/clash/arenas")]
[Route("api/clash/arenas")]
[Authorize]
public sealed class ArenasController : ControllerBase
{
    private readonly ISender _sender;

    public ArenasController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record CreateArenaApiRequest(string Name, string MetricType, int DurationDays);
    public sealed record JoinArenaApiRequest(string Code);
    public sealed record InviteFriendApiRequest(Guid FriendId);
    public sealed record RespondArenaInviteApiRequest(bool Accept);

    [HttpPost]
    public async Task<ActionResult<CreateArenaResult>> CreateArena(
        [FromBody] CreateArenaApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new CreateArenaCommand(request.Name, request.MetricType, request.DurationDays, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetArena), new { id = result.Arena?.Id }, result);
    }

    [HttpPost("join")]
    public async Task<ActionResult<JoinArenaResult>> JoinArena(
        [FromBody] JoinArenaApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new JoinArenaByCodeCommand(request.Code, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<ArenaDto>>> GetMyArenas(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = new GetUserArenasQuery(userId);
        var arenas = await _sender.Send(query, cancellationToken);

        return Ok(arenas);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ArenaLeaderboardResult>> GetArena(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = new GetArenaLeaderboardQuery(id, userId);
        var result = await _sender.Send(query, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Arena not found." });
        }

        return Ok(result);
    }

    [HttpPost("{id}/invite")]
    public async Task<ActionResult<InviteFriendToArenaResult>> InviteFriend(
        [FromRoute] string id,
        [FromBody] InviteFriendApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new InviteFriendToArenaCommand(id, request.FriendId, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveArena(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new LeaveArenaCommand(id, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.IsOwner)
            {
                return BadRequest(result);
            }
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("feed")]
    public async Task<ActionResult<IReadOnlyList<ClashActivityDto>>> GetMyArenasFeed(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var query = new GetArenaFeedQuery("all", userId);
        var feed = await _sender.Send(query, cancellationToken);
        return Ok(feed);
    }

    [HttpGet("{id}/feed")]
    public async Task<ActionResult<IReadOnlyList<ClashActivityDto>>> GetArenaFeed(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = new GetArenaFeedQuery(id, userId);
        var feed = await _sender.Send(query, cancellationToken);

        return Ok(feed);
    }

    [HttpPost("/api/v1/clash/activities/{id:guid}/kudos")]
    [HttpPost("/api/clash/activities/{id:guid}/kudos")]
    public async Task<ActionResult<SendActivityKudosResult>> SendKudos(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new SendActivityKudosCommand(id, userId);
        var result = await _sender.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("invites/{id:guid}/respond")]
    public async Task<IActionResult> RespondToInvite(
        [FromRoute] Guid id,
        [FromBody] RespondArenaInviteApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new RespondToArenaInviteCommand(id, request.Accept, userId);
        var success = await _sender.Send(command, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Arena invite not found or already processed." });
        }

        return Ok(new { success = true });
    }

    [HttpGet("invites")]
    public async Task<ActionResult<IReadOnlyList<ArenaInviteDto>>> GetMyInvites(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = new GetPendingArenaInvitesQuery(userId);
        var invites = await _sender.Send(query, cancellationToken);

        return Ok(invites);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }
}
