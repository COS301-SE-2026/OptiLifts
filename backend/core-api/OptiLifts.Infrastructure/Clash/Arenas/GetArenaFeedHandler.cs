using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Queries;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class GetArenaFeedHandler : IRequestHandler<GetArenaFeedQuery, IReadOnlyList<ClashActivityDto>>
{
    private readonly OptiLiftsDbContext _db;

    public GetArenaFeedHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ClashActivityDto>> Handle(GetArenaFeedQuery request, CancellationToken cancellationToken)
    {
        var arenaIdStr = !string.IsNullOrWhiteSpace(request.ArenaStringId)
            ? request.ArenaStringId
            : request.ArenaId.ToString();

        var arenaExists = await _db.Arenas.AsNoTracking().AnyAsync(a => a.Id == arenaIdStr, cancellationToken);
        if (!arenaExists)
        {
            return Array.Empty<ClashActivityDto>();
        }

        var activities = await _db.ClashActivities
            .AsNoTracking()
            .Where(a => a.ArenaId == arenaIdStr)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (activities.Count == 0)
        {
            return Array.Empty<ClashActivityDto>();
        }

        var userIds = activities.Select(a => a.UserId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var activityIds = activities.Select(a => a.Id).ToList();
        var userKudoedActivityIds = (await _db.ClashActivityKudos
            .AsNoTracking()
            .Where(k => activityIds.Contains(k.ActivityId) && k.SenderUserId == request.UserId)
            .Select(k => k.ActivityId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        return activities.Select(a =>
        {
            users.TryGetValue(a.UserId, out var user);
            var displayName = user?.DisplayName ?? "Athlete";

            return new ClashActivityDto(
                Id: a.Id,
                ArenaId: a.ArenaId,
                UserId: a.UserId,
                UserName: displayName,
                UserInitials: FriendshipHelpers.extractInitials(displayName),
                UserAvatarUrl: user?.ProfileImageUrl,
                EventText: a.EventText,
                Details: a.Details,
                IsPr: a.IsPr,
                IsPromotion: a.IsPromotion,
                KudosCount: a.KudosCount,
                HasUserKudoed: userKudoedActivityIds.Contains(a.Id),
                CreatedAt: a.CreatedAt
            );
        }).ToList();
    }
}
