using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Application.Scheduling.DeleteScheduledSession;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Application.Scheduling.GetScheduleAnalytics;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Application.Scheduling.UpdateMissedSessions;
using OptiLifts.Application.Scheduling.UpdateScheduledSessionStatus;
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

    public sealed record CreateScheduledSessionRequest(
        Guid WorkoutId,
        DateTime ScheduledAt,
        ScheduleStatus Status,
        string? Repeat = null,
        int? Interval = null,
        DateTime? Until = null
    );
    [HttpPost("me/schedule/sessions")]
    public async Task<ActionResult<CreateScheduledSessionResult>> CreateScheduledSession(
        [FromBody] CreateScheduledSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var command = new CreateScheduledSessionCommand(userId, request.WorkoutId, request.ScheduledAt, request.Status, request.Repeat, request.Interval, request.Until);
        var result = await _sender.Send(command, cancellationToken);
        if (result == null)
        {
            return NotFound(new
            {
                status = 404,
                title = "Not Found",
                message = "The workout was not found or not owned by the user."
            });
        }
        return CreatedAtAction(
            nameof(GetSchedule),
            new
            {
                status = result.Status
            },
            result
        );
    }

    [HttpDelete("me/schedule/sessions/{sessionId:guid}")]
    public async Task<IActionResult> DeleteScheduledSession(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var delete = await _sender.Send(new DeleteScheduledSessionCommand(userId, sessionId), cancellationToken);
        if (!delete)
        {
            return NotFound(new
            {
                status = 404,
                title = "Not Found",
                message = "The session to delete was not found or not owned by the user."
            });
        }
        return Ok(new { message = "Scheduled session deleted successfully." });
    }

    public sealed record UpdateScheduledSessionStatusRequest(ScheduleStatus Status);
    [HttpPatch("me/schedule/sessions/{sessionId:guid}")]
    public async Task<ActionResult<UpdateScheduledSessionStatusResult>> UpdateScheduledSessionStatus(
        [FromRoute] Guid sessionId,
        [FromBody] UpdateScheduledSessionStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new UpdateScheduledSessionStatusCommand(userId, sessionId, request.Status);
        var result = await _sender.Send(command, cancellationToken);
        if (result == null)
        {
            return NotFound(new
            {
                status = 404,
                title = "Not Found",
                message = "Scheduled session not found or not owned by user"
            });
        }
        return Ok(result);
    }

    [HttpPost("me/schedule/missed")]
    public async Task<ActionResult<UpdateMissedSessionsResult>> UpdateMissedSessions(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var command = new UpdateMissedSessionsCommand(userId);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    //dynamic rescheduler endpoints
    [HttpGet("me/schedule/config")]
    public async Task<ActionResult<UserScheduleConfigDto>> GetScheduleConfig(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var command = new GetUserScheduleConfigQuery(userId);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
    
    [HttpPut("me/schedule/config")]
    public async Task<ActionResult<UserScheduleConfigDto>> UpdateScheduleConfig(
        [FromBody] UserScheduleConfigDto config,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var command = new UpdateUserScheduleConfigCommand(userId, config);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/schedule/reschedule")]
    public async Task<ActionResult<RescheduleResultDto>> TriggerReschedule(
        [FromBody] RescheduleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var command = new TriggerRescheduleCommand(userId, request.SelectedMissedEntryIds);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/schedule/reschedule/confirm")]
    public async Task<ActionResult> ConfirmReschedule(
        [FromBody] List<ConfirmRescheduleItemDto> request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var command = new ConfirmRescheduleCommand(userId, request);
        await _sender.Send(command, cancellationToken);
        return Ok(new
        {
            message ="Schedule updated successfully"
        });
    }
}