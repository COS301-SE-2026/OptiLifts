using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class RejectAllFriendRequestsHandler : IRequestHandler<RejectAllFriendRequestsCommand, int>
{
    private readonly OptiLiftsDbContext _db;

    public RejectAllFriendRequestsHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<int> Handle(RejectAllFriendRequestsCommand request, CancellationToken cancellationToken)
    {
        var pending = await _db.FriendRequests.Where(r => r.ReceiverId == request.UserId && r.Status == FriendshipHelpers.statusPending).ToListAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var req in pending)
        {
            req.Status = FriendshipHelpers.statusRejected;
            req.RespondedAt = now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }
}