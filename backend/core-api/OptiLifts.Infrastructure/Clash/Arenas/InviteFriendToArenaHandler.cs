using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class InviteFriendToArenaHandler : IRequestHandler<InviteFriendToArenaCommand, InviteFriendToArenaResult>
{
    private readonly OptiLiftsDbContext _db;

    public InviteFriendToArenaHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<InviteFriendToArenaResult> Handle(InviteFriendToArenaCommand request, CancellationToken cancellationToken)
    {
        var arenaIdStr = !string.IsNullOrWhiteSpace(request.ArenaStringId)
            ? request.ArenaStringId
            : request.ArenaId.ToString();

        var arena = await _db.Arenas.FirstOrDefaultAsync(a => a.Id == arenaIdStr && a.Type == "Private", cancellationToken);
        if (arena is null)
        {
            return new InviteFriendToArenaResult(false, "Arena not found.");
        }

        var isCallerMember = await _db.ArenaMembers
            .AnyAsync(m => m.ArenaId == arena.Id && m.UserId == request.UserId, cancellationToken);
        if (!isCallerMember)
        {
            return new InviteFriendToArenaResult(false, "You must be a member of the arena to invite others.");
        }

        if (request.FriendId == request.UserId)
        {
            return new InviteFriendToArenaResult(false, "You cannot invite yourself.");
        }

        var friend = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.FriendId, cancellationToken);
        if (friend is null)
        {
            return new InviteFriendToArenaResult(false, "Friend not found.");
        }

        var (u1, u2) = FriendshipHelpers.toCanonOrder(request.UserId, request.FriendId);
        var areFriends = await _db.Friendships
            .AsNoTracking()
            .AnyAsync(f => f.UserId1 == u1 && f.UserId2 == u2, cancellationToken);
        if (!areFriends)
        {
            return new InviteFriendToArenaResult(false, "You can only invite mutual friends.");
        }

        var isAlreadyMember = await _db.ArenaMembers
            .AnyAsync(m => m.ArenaId == arena.Id && m.UserId == request.FriendId, cancellationToken);
        if (isAlreadyMember)
        {
            return new InviteFriendToArenaResult(false, "Friend is already a member of this arena.");
        }

        var existingInvite = await _db.ArenaInvites
            .AnyAsync(i => i.ArenaId == arena.Id && i.InvitedUserId == request.FriendId && i.Status == "Pending", cancellationToken);
        if (existingInvite)
        {
            return new InviteFriendToArenaResult(false, "Invitation already sent to this friend.");
        }

        var invite = new ArenaInvite
        {
            ArenaId = arena.Id,
            InvitedByUserId = request.UserId,
            InvitedUserId = request.FriendId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.ArenaInvites.Add(invite);
        await _db.SaveChangesAsync(cancellationToken);

        return new InviteFriendToArenaResult(true, "Invitation sent successfully.", invite.Id);
    }
}
