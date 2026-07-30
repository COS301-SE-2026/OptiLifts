using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Refresh;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;
namespace OptiLifts.Infrastructure.Authentication;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenHandler(OptiLiftsDbContext dbContext, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hashed = TokenHelper.HashToken(request.RefreshToken);
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == hashed, cancellationToken);

        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException();
        }

        var newAccessT = _jwtTokenService.CreateToken(user);
        var newRefreshT = TokenHelper.GenerateRefreshToken();

        user.RefreshTokenHash = TokenHelper.HashToken(newRefreshT);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            newAccessT,
            newRefreshT,
            new AuthUserDto(user.Id, user.DisplayName, user.Email, user.CreatedAt, user.Metric, user.LightTheme)
        );
    }
}