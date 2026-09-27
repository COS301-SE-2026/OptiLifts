using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Application.Clash.Duels.Queries;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/clash/duels")]
[Authorize]
public sealed class DuelsController : ControllerBase
{
    private readonly ISender _sender;

    public DuelsController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record CreateDuelApiRequest(Guid RivalUserId, string ExerciseName, Guid? ExerciseId, string TargetType, int DurationDays);
    public sealed record RespondDuelApiRequest(bool Accept);

    [HttpPost]
    public async Task<ActionResult<CreateDuelChallengeResult>> CreateDuel([FromBody] CreateDuelApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new CreateDuelChallengeCommand(userId, request.RivalUserId, request.ExerciseName, request.ExerciseId, request.TargetType, request.DurationDays), cancellationToken);
        if (!res.Success)
        {
            return BadRequest(res);
        }
        return Ok(res);
    }

    [HttpGet("invites")]
    public async Task<ActionResult<IReadOnlyList<DuelInviteDto>>> GetInvites(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new GetPendingDuelInvitesQuery(userId), cancellationToken);
        return Ok(res);
    }

    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> Respond([FromRoute] Guid id, [FromBody] RespondDuelApiRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(new RespondToDuelCommand(userId, id, request.Accept), cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Duel not found or already resolved" });
        }

        return Ok(new { success = true });
    }

    [HttpGet]
    public async Task<ActionResult<UserDuelsResult>> GetDuels([FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(new GetUserDuelsQuery(userId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DuelDetailDto>> GetDuelDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new GetDuelDetailQuery(userId, id), cancellationToken);

        if (res is null)
        {
            return NotFound();
        }

        return Ok(res);
    }

    [HttpPost("{id:guid}/hype")]
    public async Task<ActionResult<SendDuelHypeResult>> SendHype([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var res = await _sender.Send(new SendDuelHypeCommand(userId, id), cancellationToken);

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
}
