using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Me;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Authentication;

public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, AuthUserDto>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetCurrentUserHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException();
        }

        return new AuthUserDto(user.Id, user.DisplayName, user.Email, user.CreatedAt, user.Metric, user.LightTheme);
    }
}