using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Queries;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class GetPendingArenaInvitesHandler : IRequestHandler<GetPendingArenaInvitesQuery, IReadOnlyList<ArenaInviteDto>>
{
    private readonly OptiLiftsDbContext _db;

    public GetPendingArenaInvitesHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ArenaInviteDto>> Handle(GetPendingArenaInvitesQuery request, CancellationToken cancellationToken)
    {
        var invites = await _db.ArenaInvites
            .AsNoTracking()
            .Where(i => i.InvitedUserId == request.UserId && i.Status == "Pending")
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (invites.Count == 0)
        {
            return Array.Empty<ArenaInviteDto>();
        }

        var arenaIds = invites.Select(i => i.ArenaId).Distinct().ToList();
        var senderIds = invites.Select(i => i.InvitedByUserId).Distinct().ToList();

        var arenas = await _db.Arenas
            .AsNoTracking()
            .Where(a => arenaIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var senders = await _db.Users
            .AsNoTracking()
            .Where(u => senderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return invites.Select(i =>
        {
            arenas.TryGetValue(i.ArenaId, out var arena);
            senders.TryGetValue(i.InvitedByUserId, out var sender);

            var senderName = sender?.DisplayName ?? "Friend";

            return new ArenaInviteDto(
                Id: i.Id,
                ArenaId: i.ArenaId,
                ArenaName: arena?.Name ?? "Private Squad",
                ArenaCode: arena?.Code,
                InvitedByUserId: i.InvitedByUserId,
                InvitedByName: senderName,
                InvitedByInitials: FriendshipHelpers.extractInitials(senderName),
                InvitedByAvatarUrl: sender?.ProfileImageUrl,
                Status: i.Status,
                CreatedAt: i.CreatedAt
            );
        }).ToList();
    }
}
