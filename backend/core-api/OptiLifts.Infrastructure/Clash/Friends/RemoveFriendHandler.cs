using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class RemoveFriendHandler : IRequestHandler<RemoveFriendCommand, bool>
{
    private readonly OptiLiftsDbContext _db;

    public RemoveFriendHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(RemoveFriendCommand request, CancellationToken cancellationToken)
    {
        var (u1, u2) = FriendshipHelpers.toCanonOrder(request.UserId, request.FriendId);
        var friendship = await _db.Friendships.FirstOrDefaultAsync(f => f.UserId1 == u1 && f.UserId2 == u2, cancellationToken);
        if (friendship is null)
        {
            return false;
        }
        _db.Friendships.Remove(friendship);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}