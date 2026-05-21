using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Authentication;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email) : base($"Email already in use: {email}") { }
}

public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterUserHandler(OptiLiftsDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var trimmedEmail = request.Email.Trim();

        var exists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == trimmedEmail, cancellationToken);

        if (exists)
        {
            throw new DuplicateEmailException(trimmedEmail);
        }

        var hash = _passwordHasher.Hash(request.Password);

        var user = new User
        {
            Email = trimmedEmail,
            PasswordHash = hash,
            DisplayName = request.DisplayName?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.CreateToken(user);

        var dto = new AuthResponseDto(
            token,
            new AuthUserDto(user.Id, user.DisplayName, user.Email, user.CreatedAt)
        );

        return dto;
    }
}
