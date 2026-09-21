using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OptiLifts.API.RateLimiting;
using OptiLifts.Application.Vision;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Ai)]
public class VisionController : ControllerBase
{
    private readonly ISender _sender;

    public VisionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        [FromBody] VisionAnalyzeRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Exercise))
        {
            return BadRequest(new { message = "Exercise and valid request payload are required." });
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userIdClaim))
            {
                request.UserId = userIdClaim;
            }
            else
            {
                return BadRequest(new { message = "UserId is required." });
            }
        }

        var jobId = await _sender.Send(new AnalyzeVisionCommand(request), cancellationToken);
        return Ok(new { jobId });
    }

    [HttpPost("worker-result")]
    public async Task<IActionResult> WorkerResult(
        [FromBody] VisionWorkerResult request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.JobId))
        {
            return BadRequest(new { message = "JobId is required." });
        }

        var success = await _sender.Send(new ProcessWorkerResultCommand(request), cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Job with ID '{request.JobId}' not found." });
        }

        return Ok(new { success = true });
    }

    [HttpGet("result/{jobId}")]
    public async Task<IActionResult> GetResult(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return BadRequest(new { message = "JobId cannot be empty." });
        }

        var result = await _sender.Send(new GetVisionResultQuery(jobId), cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Job with ID '{jobId}' not found." });
        }

        return Ok(result);
    }
}
