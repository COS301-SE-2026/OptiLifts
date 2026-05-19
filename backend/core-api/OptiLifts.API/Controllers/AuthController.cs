using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Application.Auth.Login;
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
            return Ok(result);
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
            return Ok(result);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { title = "Unauthorized", status = 401 });
        }
    }
}
