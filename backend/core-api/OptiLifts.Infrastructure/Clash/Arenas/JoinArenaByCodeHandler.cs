using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class JoinArenaByCodeHandler : IRequestHandler<JoinArenaByCodeCommand, JoinArenaResult>
{
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public JoinArenaByCodeHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task<JoinArenaResult> Handle(JoinArenaByCodeCommand request, CancellationToken cancellationToken)
    {
        var code = ArenaCodeHelper.NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(code))
        {
            return new JoinArenaResult(false, "Arena code cannot be empty.");
        }

        var arena = await _db.Arenas.FirstOrDefaultAsync(a => a.Code == code && a.Type == "Private", cancellationToken);
        if (arena is null)
        {
            return new JoinArenaResult(false, "No arena found with the specified code.");
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return new JoinArenaResult(false, "User not found.");
        }
        if (string.IsNullOrWhiteSpace(user.Weight) || !float.TryParse(user.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var bw) || bw <= 0f)
        {
            return new JoinArenaResult(false, "Please set your bodyweight in your profile before joining an arena.");
        }

        var isAlreadyMember = await _db.ArenaMembers
            .AnyAsync(m => m.ArenaId == arena.Id && m.UserId == request.UserId, cancellationToken);

        if (isAlreadyMember)
        {
            return new JoinArenaResult(false, "You are already a member of this arena.");
        }

        if (arena.SeasonEndDate <= DateTime.UtcNow)
        {
            return new JoinArenaResult(false, "This arena season has ended.");
        }

        // Check if there was an existing pending invite
        var pendingInvite = await _db.ArenaInvites
            .FirstOrDefaultAsync(i => i.ArenaId == arena.Id && i.InvitedUserId == request.UserId && i.Status == "Pending", cancellationToken);
        if (pendingInvite != null)
        {
            pendingInvite.Status = "Accepted";
        }

        var now = DateTime.UtcNow;
        var member = new ArenaMember
        {
            ArenaId = arena.Id,
            UserId = request.UserId,
            Role = "Member",
            JoinedAt = now
        };

        var activity = new ClashActivity
        {
            ArenaId = arena.Id,
            UserId = request.UserId,
            EventText = $"{user.DisplayName} joined the squad!",
            Details = $"Joined {arena.Name}",
            IsPr = false,
            IsPromotion = false,
            KudosCount = 0,
            CreatedAt = now
        };

        _db.ArenaMembers.Add(member);
        _db.ClashActivities.Add(activity);

        await _db.SaveChangesAsync(cancellationToken);
        await ClashFeedHelper.PruneArenaActivitiesAsync(_db, arena.Id, cancellationToken);

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

        var memberCount = await _db.ArenaMembers.CountAsync(m => m.ArenaId == arena.Id, cancellationToken);
        var arenaDto = ClashFeedHelper.ToArenaDto(arena, memberCount, userRole: "Member", isUserMember: true);

        return new JoinArenaResult(true, "Successfully joined arena.", arenaDto);
    }
}
