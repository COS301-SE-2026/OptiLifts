using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Notifications;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class UpdateProfileDetailsHandler : IRequestHandler<UpdateProfileDetailsCommand>
{

    private readonly OptiLiftsDbContext _dbContext;
    private readonly IPublisher? _publisher;

    public UpdateProfileDetailsHandler(OptiLiftsDbContext dbContext, IPublisher? publisher = null)
    {
        _dbContext = dbContext;
        _publisher = publisher;
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

        if (_publisher is not null)
        {
            await _publisher.Publish(new UserProfileUpdatedNotification(request.UserId), cancellationToken);
        }
    }
}
