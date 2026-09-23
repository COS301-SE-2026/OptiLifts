using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class RespondToArenaInviteHandler : IRequestHandler<RespondToArenaInviteCommand, bool>
{
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public RespondToArenaInviteHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task<bool> Handle(RespondToArenaInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _db.ArenaInvites
            .FirstOrDefaultAsync(i => i.Id == request.InviteId && i.InvitedUserId == request.UserId && i.Status == "Pending", cancellationToken);

        if (invite is null)
        {
            return false;
        }

        invite.Status = request.Accept ? "Accepted" : "Rejected";

        if (request.Accept)
        {
            var arena = await _db.Arenas.FirstOrDefaultAsync(a => a.Id == invite.ArenaId, cancellationToken);
            if (arena != null)
            {
                var isMember = await _db.ArenaMembers
                    .AnyAsync(m => m.ArenaId == arena.Id && m.UserId == request.UserId, cancellationToken);

                if (!isMember)
                {
                    var now = DateTime.UtcNow;
                    var member = new ArenaMember
                    {
                        ArenaId = arena.Id,
                        UserId = request.UserId,
                        Role = "Member",
                        JoinedAt = now
                    };

                    var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
                    var userName = user?.DisplayName ?? "An athlete";

                    var activity = new ClashActivity
                    {
                        ArenaId = arena.Id,
                        UserId = request.UserId,
                        EventText = $"{userName} joined the squad!",
                        Details = $"Accepted invitation to {arena.Name}",
                        IsPr = false,
                        IsPromotion = false,
                        KudosCount = 0,
                        CreatedAt = now
                    };

                    _db.ArenaMembers.Add(member);
                    _db.ClashActivities.Add(activity);

                    await _db.SaveChangesAsync(cancellationToken);
                    await ClashFeedHelper.PruneArenaActivitiesAsync(_db, arena.Id, cancellationToken);

                    if (user != null)
                    {
                        var activityDto = new ClashActivityDto(
                            Id: activity.Id,
                            ArenaId: arena.Id,
                            UserId: user.Id,
                            UserName: user.DisplayName,
                            UserInitials: FriendshipHelpers.extractInitials(user.DisplayName),
                            UserAvatarUrl: user.ProfileImageUrl,
                            EventText: activity.EventText,
                            Details: activity.Details,
                            IsPr: activity.IsPr,
                            IsPromotion: activity.IsPromotion,
                            KudosCount: activity.KudosCount,
                            HasUserKudoed: false,
                            CreatedAt: activity.CreatedAt
                        );

                        await _notifier.BroadcastActivityAsync(arena.Id, activityDto, cancellationToken);
                        await _notifier.BroadcastUserJoinedAsync(arena.Id, user.DisplayName, cancellationToken);
                    }

                    return true;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
