using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Application.Clash.Friends.Commands;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class RespondToFriendRequestHandler : IRequestHandler<RespondToFriendRequestCommand, bool>{
    private readonly OptiLiftsDbContext _db;

    public RespondToFriendRequestHandler(OptiLiftsDbContext db){
        _db = db;
    }

    public async Task<bool> Handle(RespondToFriendRequestCommand request, CancellationToken cancellationToken){
        var friendReq = await _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ReceiverId == request.UserId && r.Status == FriendshipHelpers.statusPending, cancellationToken);
        if (friendReq is null){
            return false;
        }

        friendReq.RespondedAt = DateTime.UtcNow;
        if (request.Accept){
            friendReq.Status = FriendshipHelpers.statusAccepted;
            var (u1, u2) = FriendshipHelpers.toCanonOrder(friendReq.SenderId, friendReq.ReceiverId);
            var exists = await _db.Friendships.AnyAsync(f => f.UserId1 == u1 && f.UserId2 == u2, cancellationToken);
            if (!exists){
                _db.Friendships.Add(new Friendship{
                    UserId1 = u1,
                    UserId2 = u2,
                    CreatedAt = DateTime.UtcNow
                });
            }
        } else {
            friendReq.Status = FriendshipHelpers.statusRejected;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}