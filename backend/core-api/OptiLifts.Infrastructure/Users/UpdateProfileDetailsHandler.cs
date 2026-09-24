using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class UpdateProfileDetailsHandler : IRequestHandler<UpdateProfileDetailsCommand>
{

    private readonly OptiLiftsDbContext _dbContext;
    private readonly ISender? _sender;

    public UpdateProfileDetailsHandler(OptiLiftsDbContext dbContext, ISender? sender = null)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Handle(UpdateProfileDetailsCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.DisplayName = request.DisplayName;
        user.Bio = request.Bio;
        user.Sex = request.Sex;
        user.DateOfBirth = request.DateOfBirth;
        user.Weight = request.Weight?.ToString(CultureInfo.InvariantCulture);
        user.Height = request.Height?.ToString(CultureInfo.InvariantCulture);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_sender is not null)
        {
            await _sender.Send(new UserProfileUpdatedCommand(request.UserId), cancellationToken);
        }
    }
}