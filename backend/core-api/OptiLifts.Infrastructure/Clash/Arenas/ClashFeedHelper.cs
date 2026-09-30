using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public static class ClashFeedHelper
{
    public const int MaxFeedItemsPerArena = 100;

    public static async Task PruneArenaActivitiesAsync(OptiLiftsDbContext db, string arenaId, CancellationToken cancellationToken = default)
    {
        var total = await db.ClashActivities.CountAsync(a => a.ArenaId == arenaId, cancellationToken);
        if (total > MaxFeedItemsPerArena)
        {
            var excess = await db.ClashActivities
                .Where(a => a.ArenaId == arenaId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip(MaxFeedItemsPerArena)
                .ToListAsync(cancellationToken);

            if (excess.Count > 0)
            {
                db.ClashActivities.RemoveRange(excess);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public static ArenaDto ToArenaDto(Arena arena, int memberCount, string? userRole, bool isUserMember)
    {
        var now = DateTime.UtcNow;
        var daysRemaining = Math.Max(0, (int)Math.Ceiling((arena.SeasonEndDate - now).TotalDays));
        var isActive = arena.SeasonEndDate > now;

        return new ArenaDto(
            Id: arena.Id,
            Name: arena.Name,
            Type: arena.Type,
            Code: arena.Code,
            CreatedById: arena.CreatedById,
            MetricType: arena.MetricType,
            DurationDays: arena.DurationDays,
            SeasonEndDate: arena.SeasonEndDate,
            CreatedAt: arena.CreatedAt,
            MemberCount: memberCount,
            IsActive: isActive,
            DaysRemaining: daysRemaining,
            UserRole: userRole,
            IsUserMember: isUserMember
        );
    }
}
