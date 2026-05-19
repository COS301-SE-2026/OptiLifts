using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Login;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Infrastructure.Database;

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
        var email = request.Email?.Trim();

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            throw new InvalidCredentialsException();
        }

        var validP = _passwordHasher.Verify(user.PasswordHash, request.Password);

        if (!validP)
        {
            throw new InvalidCredentialsException();
        }

        var token = _jwtTokenService.CreateToken(user);

        return new AuthResponseDto(token, new AuthUserDto(user.Id, user.DisplayName, user.Email, user.CreatedAt));
    }
}
