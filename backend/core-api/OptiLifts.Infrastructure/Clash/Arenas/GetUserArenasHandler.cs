using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Queries;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class GetUserArenasHandler : IRequestHandler<GetUserArenasQuery, IReadOnlyList<ArenaDto>>
{
    private readonly OptiLiftsDbContext _db;

    public GetUserArenasHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ArenaDto>> Handle(GetUserArenasQuery request, CancellationToken cancellationToken)
    {
        var memberships = await _db.ArenaMembers
            .AsNoTracking()
            .Where(m => m.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return Array.Empty<ArenaDto>();
        }

        var arenaIds = memberships.Select(m => m.ArenaId).Distinct().ToList();
        var arenas = await _db.Arenas
            .AsNoTracking()
            .Where(a => arenaIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var memberCounts = await _db.ArenaMembers
            .AsNoTracking()
            .Where(m => arenaIds.Contains(m.ArenaId))
            .GroupBy(m => m.ArenaId)
            .Select(g => new { ArenaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ArenaId, g => g.Count, cancellationToken);

        var membershipMap = memberships.ToDictionary(m => m.ArenaId, m => m.Role);

        return arenas
            .OrderByDescending(a => a.SeasonEndDate > DateTime.UtcNow)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a =>
            {
                var count = memberCounts.GetValueOrDefault(a.Id, 1);
                var role = membershipMap.GetValueOrDefault(a.Id, "Member");
                return ClashFeedHelper.ToArenaDto(a, count, role, isUserMember: true);
            })
            .ToList();
    }
}
