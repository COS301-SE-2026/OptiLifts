using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Google;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Infrastructure.Authentication;

public sealed class GoogleAuthHandler : IRequestHandler<GoogleAuthCommand, AuthResponseDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenService _jwtTokenService;

    public GoogleAuthHandler(
        OptiLiftsDbContext dbContext,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _googleAuthService = googleAuthService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new ArgumentException("Google ID token is required.", nameof(request.IdToken));
        }

        var googleUser = await _googleAuthService.ValidateIdTokenAsync(request.IdToken, cancellationToken);
        var email = googleUser.Email.Trim();
        var emailHash = EmailHasher.HashEmail(email);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => (u.GoogleId != null && u.GoogleId == googleUser.GoogleId) || u.EmailHash == emailHash, cancellationToken);

        if (user != null)
        {
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleUser.GoogleId;
            }

            if (string.IsNullOrWhiteSpace(user.DisplayName) && !string.IsNullOrWhiteSpace(googleUser.Name))
            {
                user.DisplayName = googleUser.Name.Trim();
            }

            if (string.IsNullOrWhiteSpace(user.ProfileImageUrl) && !string.IsNullOrWhiteSpace(googleUser.PictureUrl))
            {
                user.ProfileImageUrl = googleUser.PictureUrl;
            }
        }
        else
        {
            var displayName = !string.IsNullOrWhiteSpace(googleUser.Name)
                ? googleUser.Name.Trim()
                : (email.Contains('@') ? email.Split('@')[0] : "User");

            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                EmailHash = emailHash,
                GoogleId = googleUser.GoogleId,
                PasswordHash = null,
                DisplayName = displayName,
                ProfileImageUrl = googleUser.PictureUrl,
                CreatedAt = DateTime.UtcNow,
                Level = 1,
                Metric = true,
                LightTheme = false
            };

            _dbContext.Users.Add(user);
        }

        var token = _jwtTokenService.CreateToken(user);
        var refreshToken = TokenHelper.GenerateRefreshToken();

        user.RefreshTokenHash = TokenHelper.HashToken(refreshToken);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            token,
            refreshToken,
            new AuthUserDto(user.Id, user.DisplayName, user.Email, user.CreatedAt, user.Metric, user.LightTheme)
        );
    }
}
