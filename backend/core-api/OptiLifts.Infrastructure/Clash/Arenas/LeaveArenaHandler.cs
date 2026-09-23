using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class LeaveArenaHandler : IRequestHandler<LeaveArenaCommand, LeaveArenaResult>
{
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public LeaveArenaHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task<LeaveArenaResult> Handle(LeaveArenaCommand request, CancellationToken cancellationToken)
    {
        var arenaIdStr = !string.IsNullOrWhiteSpace(request.ArenaStringId)
            ? request.ArenaStringId
            : request.ArenaId.ToString();

        var arena = await _db.Arenas.FirstOrDefaultAsync(a => a.Id == arenaIdStr, cancellationToken);
        if (arena is null)
        {
            return new LeaveArenaResult(false, "Arena not found.");
        }

        var member = await _db.ArenaMembers
            .FirstOrDefaultAsync(m => m.ArenaId == arena.Id && m.UserId == request.UserId, cancellationToken);

        if (member is null)
        {
            return new LeaveArenaResult(false, "You are not a member of this arena.");
        }

        if (string.Equals(member.Role, "Owner", StringComparison.OrdinalIgnoreCase))
        {
            return new LeaveArenaResult(false, "Owners cannot leave the arena. Transfer ownership or delete the squad.", IsOwner: true);
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        var userName = user?.DisplayName ?? "An athlete";

        _db.ArenaMembers.Remove(member);

        var now = DateTime.UtcNow;
        var activity = new ClashActivity
        {
            ArenaId = arena.Id,
            UserId = request.UserId,
            EventText = $"{userName} left the squad.",
            Details = $"Left {arena.Name}",
            IsPr = false,
            IsPromotion = false,
            KudosCount = 0,
            CreatedAt = now
        };

        _db.ClashActivities.Add(activity);
        await _db.SaveChangesAsync(cancellationToken);
        await ClashFeedHelper.PruneArenaActivitiesAsync(_db, arena.Id, cancellationToken);

        await _notifier.BroadcastUserLeftAsync(arena.Id, userName, cancellationToken);

        return new LeaveArenaResult(true, "Successfully left the arena.");
    }
}
