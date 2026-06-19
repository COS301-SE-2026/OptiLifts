using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Logout;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Authentication;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly OptiLiftsDbContext _dbContext;
    public LogoutHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
        .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user != null)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiryTime = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
