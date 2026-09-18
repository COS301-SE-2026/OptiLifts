using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Application.Clash.Friends.Queries;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/clash/friends")]
[Authorize]
public sealed class FriendsController : ControllerBase
{
    private readonly ISender _sender;

    public FriendsController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record SendFriendRequestApiRequest(string FriendCode);
    public sealed record RespondFriendRequestApiRequest(bool Accept);

    [HttpGet("code")]
    public async Task<ActionResult<object>> GetMyCode(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var code = await _sender.Send(new GetMyFriendCodeQuery(userId), cancellationToken);
        if (code is null)
        {
            return NotFound();
        }
        return Ok(new
        {
            code
        });
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FriendDto>>> GetFriends(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var friends = await _sender.Send(new GetFriendsListQuery(userId), cancellationToken);
        return Ok(friends);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<PendingFriendRequestsResult>> GetRequests(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var requests = await _sender.Send(new GetPendingFriendRequestsQuery(userId), cancellationToken);
        return Ok(requests);
    }

    [HttpPost("requests")]
    public async Task<ActionResult<SendFriendRequestResult>> SendRequest(
        [FromBody] SendFriendRequestApiRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var result = await _sender.Send(new SendFriendRequestCommand(userId, request.FriendCode), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("requests/{id:guid}/respond")]
    public async Task<IActionResult> RespondToRequest(
        [FromRoute] Guid id,
        [FromBody] RespondFriendRequestApiRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(new RespondToFriendRequestCommand(userId, id, request.Accept), cancellationToken);
        if (!success)
        {
            return NotFound(new
            {
                message = "Friend request not found or already processed"
            });
        }

        return Ok(new
        {
            success = true
        });
    }

    [HttpPost("requests/reject-all")]
    public async Task<IActionResult> RejectAllRequests(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var rejectCount = await _sender.Send(new RejectAllFriendRequestsCommand(userId), cancellationToken);
        return Ok(new
        {
            rejectCount
        });
    }
    [HttpDelete("{friendId:guid}")]
    public async Task<IActionResult> RemoveFriend(
        [FromRoute] Guid friendId,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var success = await _sender.Send(new RemoveFriendCommand(userId, friendId), cancellationToken);
        if (!success)
        {
            return NotFound(new
            {
                message = "Friendship not found"
            });
        }
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

}