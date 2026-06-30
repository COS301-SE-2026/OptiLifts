using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Application.Scheduling.GetScheduleAnalytics;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class SchedulesController : ControllerBase
{
    private readonly ISender _sender;
    public SchedulesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me/schedule")]
    public async Task<ActionResult<IReadOnlyList<ScheduledEntryDto>>> GetSchedule(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] ScheduleStatus? status,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var query = new GetScheduleQuery(userId, startDate, endDate, status);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

    [HttpGet("me/schedule/analytics")]
    public async Task<ActionResult<ScheduleAnalyticsDto>> GetScheduleAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] ScheduleStatus? status,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var query = new GetScheduleAnalyticsQuery(userId, startDate, endDate, status);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}