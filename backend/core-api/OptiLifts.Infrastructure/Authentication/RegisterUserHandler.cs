using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

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
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email and password must be provided");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password must be provided");
        }

        var trimmedEmail = request.Email.Trim();
        var emailHash = EmailHasher.HashEmail(trimmedEmail);

        var exists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.EmailHash == emailHash, cancellationToken);

        if (exists)
        {
            throw new DuplicateEmailException(trimmedEmail);
        }

        var hash = _passwordHasher.Hash(request.Password);

        var user = new User
        {
            Email = trimmedEmail,
            EmailHash = emailHash,
            PasswordHash = hash,
            DisplayName = request.DisplayName?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.CreateToken(user);
        var refreshToken= TokenHelper.GenerateRefreshToken();

        user.RefreshTokenHash = TokenHelper.HashToken(refreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new AuthResponseDto(
            token,
            refreshToken,
            new AuthUserDto(user.Id, user.DisplayName, user.Email, user.CreatedAt)
        );

        return dto;
    }
}
