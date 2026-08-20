using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Users;

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

    [HttpPatch("profilePicture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile profilePicture, CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        if (profilePicture == null || profilePicture.Length == 0)
        {
            return BadRequest("No file uploaded or file is empty.");
        }

        if (profilePicture.ContentType != "image/jpeg" && profilePicture.ContentType != "image/png" && profilePicture.ContentType != "image/webp")
        {
            return BadRequest("File must be an image(JPEG, PNG or WebP)");
        }

        try
        {
            using var stream = profilePicture.OpenReadStream();
            var command = new UploadProfilePictureCommand(
                userId,
                stream,
                profilePicture.FileName,
                profilePicture.ContentType
            );
            var imageUrl = await _sender.Send(command, cancellationToken);
            return Ok(new { profilePictureUrl = imageUrl });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("deleteProfilePicture")]
    public async Task<IActionResult> DeleteProfilePicture(CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            await _sender.Send(new DeleteProfilePictureCommand(userId), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public sealed record UserDetailsRequest(
        string DisplayName,
        string? Bio,
        string? Sex,
        string? DateOfBirth,
        double? Weight,
        double? Height
    );

    [HttpPatch("profileDetails")]
    public async Task<IActionResult> UpdateProfileDetails([FromBody] UserDetailsRequest request, CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest("Display name is required.");
        }

        try
        {
            var command = new UpdateProfileDetailsCommand(
                userId,
                request.DisplayName,
                request.Bio,
                request.Sex,
                request.DateOfBirth,
                request.Weight,
                request.Height
            );

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public sealed record PreferencesRequest(
        string Theme,
        string Units
    );

    [HttpPatch("preferences")]
    public async Task<IActionResult> UpdateUserPreferences([FromBody] PreferencesRequest request, CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request.Theme) || string.IsNullOrEmpty(request.Units))
        {
            return BadRequest("Theme and units are required.");
        }

        try
        {
            var command = new UpdateUserPreferencesCommand(
                userId,
                request.Theme,
                request.Units
            );
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public sealed record UpdatePasswordRequest(
        string CurrentPassword,
        string NewPassword
    );

    [HttpPost("updatePassword")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest("Current password and new password are required.");
        }

        try
        {
            var command = new UpdatePasswordCommand(
                userId,
                request.CurrentPassword,
                request.NewPassword
            );

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException e)
        {
            return BadRequest(new { error = e.Message });
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    public sealed record SetPasswordRequest(
        string NewPassword
    );

    [HttpPost("setPassword")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest("New password is required.");
        }

        try
        {
            var command = new SetPasswordCommand(
                userId,
                request.NewPassword
            );

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { error = e.Message });
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var userIdstr = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdstr) || !Guid.TryParse(userIdstr, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            await _sender.Send(new DeleteAccountCommand(userId), cancellationToken);

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

}