using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Login;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Infrastructure.Authentication;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid credentials") { }
}

public sealed class LoginUserHandler : IRequestHandler<LoginUserCommand, AuthResponseDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUserHandler(OptiLiftsDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var email = request.Email.Trim();
        var emailHash = EmailHasher.HashEmail(email);

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.EmailHash == emailHash, cancellationToken);

        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var validP = _passwordHasher.Verify(user.PasswordHash, request.Password);

        if (!validP)
        {
            throw new InvalidCredentialsException();
        }

        var token = _jwtTokenService.CreateToken(user);
        var refreshToken = TokenHelper.GenerateRefreshToken();

        user.RefreshTokenHash = TokenHelper.HashToken(refreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(token, refreshToken, new AuthUserDto(user.Id, user.DisplayName, user.Email, user.CreatedAt, user.Metric, user.LightTheme));
    }
}
