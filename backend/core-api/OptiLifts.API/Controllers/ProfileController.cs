using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Profile;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly ISender _sender;

    public ProfileController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ProfileOverviewDto>> GetOverview(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _sender.Send(new GetProfileOverviewQuery(userId), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<ProfileCalendarDto>> GetCalendar(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var resolvedYear = year ?? now.Year;
        var resolvedMonth = month ?? now.Month;

        if (resolvedMonth is < 1 or > 12)
        {
            return BadRequest(new { title = "Invalid month", status = 400 });
        }

        try
        {
            var result = await _sender.Send(new GetProfileCalendarQuery(userId, resolvedYear, resolvedMonth), cancellationToken);
            return Ok(result);
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new { title = "Invalid calendar month", status = 400 });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }
}