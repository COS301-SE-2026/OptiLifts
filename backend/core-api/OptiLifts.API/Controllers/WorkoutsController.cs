using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using OptiLifts.Application.Workouts.GetWorkouts;

namespace OptiLifts.API.Controllers;

//http entrypoint for workout related api calls
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WorkoutsController : ControllerBase {
    private readonly ISender _sender;

    public WorkoutsController(ISender sender) {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkoutCardDto>>> GetWorkouts(CancellationToken cancellationToken){
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId)) {
            return Unauthorized();
        }

        var result = await _sender.Send(new GetWorkoutsQuery(userId), cancellationToken);
        return Ok(result);
    }
}
