using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Users;
using System.Security.Claims;
namespace OptiLifts.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
public sealed class UserSettingsController : ControllerBase
{
    private readonly ISender _sender;
    public UserSettingsController(ISender sender)
    {
        _sender = sender;
    }


    [HttpGet("settings")]
    public async Task<ActionResult<UserSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var settings = await _sender.Send(new GetUserSettingsQuery(userId), cancellationToken);
            return Ok(settings);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}