using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class UpdateUserPreferencesHandler : IRequestHandler<UpdateUserPreferencesCommand>
{
    private readonly OptiLiftsDbContext _dbContext;

    public UpdateUserPreferencesHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.LightTheme = string.Equals(request.Theme, "light", StringComparison.OrdinalIgnoreCase);
        user.Metric = string.Equals(request.Units, "metric", StringComparison.OrdinalIgnoreCase);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}