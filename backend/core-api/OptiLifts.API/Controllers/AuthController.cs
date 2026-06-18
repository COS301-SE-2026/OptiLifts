using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Auth.Login;
using OptiLifts.Application.Auth.Logout;
using OptiLifts.Application.Auth.Me;
using OptiLifts.Application.Auth.Refresh;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Infrastructure.Authentication;

namespace OptiLifts.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsProduction(),
            SameSite = SameSiteMode.Lax, //should send cookies with cross site requests for top navigation
            Path = "/",
            Expires = DateTime.UtcNow.AddHours(2)
        };

        Response.Cookies.Append("access_token", accessToken, cookieOptions);
        cookieOptions.Expires = DateTime.UtcNow.AddDays(7);
        Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
    }
    private void ClearTokenCookies()
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsProduction(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTime.UtcNow.AddDays(-1) //setting expiration date to yesterdya deletes cookie
        };

        Response.Cookies.Append("access_token", "", cookieOptions);
        Response.Cookies.Append("refresh_token", "", cookieOptions);
    }

    public sealed record RegisterRequest(string DisplayName, string Email, string Password);
    public sealed record LoginRequest(string Email, string Password);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest();
        }

        try
        {
            var result = await _sender.Send(new RegisterUserCommand(request.DisplayName, request.Email, request.Password), cancellationToken);
            SetTokenCookies(result.AccessToken, result.RefreshToken);
            return Ok(result.User);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new { title = "Email already in use", status = 409 });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest();
        }

        try
        {
            var result = await _sender.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);
            SetTokenCookies(result.AccessToken, result.RefreshToken);
            return Ok(result.User);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { title = "Unauthorized", status = 401 });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var user = await _sender.Send(new GetCurrentUserQuery(Guid.Parse(userId)), cancellationToken);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
            SetTokenCookies(result.AccessToken, result.RefreshToken);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            ClearTokenCookies();
            return Unauthorized();
        }
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            ClearTokenCookies();
            return Ok();
        }

        await _sender.Send(new LogoutCommand(Guid.Parse(userId)), cancellationToken);

        ClearTokenCookies();
        return Ok();
    }
}
