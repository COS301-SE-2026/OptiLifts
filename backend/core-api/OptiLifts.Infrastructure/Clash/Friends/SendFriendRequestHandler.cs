using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class SendFriendRequestHandler : IRequestHandler<SendFriendRequestCommand, SendFriendRequestResult>
{
    private readonly OptiLiftsDbContext _db;

    public SendFriendRequestHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<SendFriendRequestResult> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var targetCode = (request.FriendCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(targetCode))
        {
            return new SendFriendRequestResult(false, "Friend code cannot be empty");
        }

        var receiver = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.FriendCode == targetCode, cancellationToken);
        if (receiver is null)
        {
            return new SendFriendRequestResult(false, "No user found with that friend code");
        }

        if (receiver.Id == request.UserId)
        {
            return new SendFriendRequestResult(false, "You cannot send a friend request to yourself");
        }

        var (u1, u2) = FriendshipHelpers.toCanonOrder(request.UserId, receiver.Id);
        var areFriends = await _db.Friendships.AsNoTracking().AnyAsync(f => f.UserId1 == u1 && f.UserId2 == u2, cancellationToken);
        if (areFriends)
        {
            return new SendFriendRequestResult(false, "You are already friends with this user");
        }

        var pendingArli = await _db.FriendRequests.AsNoTracking().AnyAsync(r => r.SenderId == request.UserId && r.ReceiverId == receiver.Id && r.Status == FriendshipHelpers.statusPending, cancellationToken);
        if (pendingArli)
        {
            return new SendFriendRequestResult(false, "You have already sent a friend request to this user");
        }

        var friendReq = new FriendRequest
        {
            SenderId = request.UserId,
            ReceiverId = receiver.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(friendReq);
        await _db.SaveChangesAsync(cancellationToken);
        return new SendFriendRequestResult(true, "Friend request sent successfully", friendReq.Id);
    }
}