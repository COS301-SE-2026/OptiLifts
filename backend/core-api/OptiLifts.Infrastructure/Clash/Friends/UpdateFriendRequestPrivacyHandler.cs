using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class UpdateFriendRequestPrivacyHandler : IRequestHandler<UpdateFriendRequestPrivacyCommand, bool>
{
    private readonly OptiLiftsDbContext _db;

    public UpdateFriendRequestPrivacyHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateFriendRequestPrivacyCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }
        var normalised = request.Privacy.Equals("none", StringComparison.OrdinalIgnoreCase) ? "None" : "Everyone";
        user.FriendRequestPrivacy = normalised;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
