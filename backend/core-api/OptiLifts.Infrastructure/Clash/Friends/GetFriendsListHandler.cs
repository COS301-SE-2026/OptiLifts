using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class GetFriendsListHandler : IRequestHandler<GetFriendsListQuery, IReadOnlyList<FriendDto>>
{
    private readonly OptiLiftsDbContext _db;

    public GetFriendsListHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FriendDto>> Handle(GetFriendsListQuery request, CancellationToken cancellationToken)
    {
        var friendships = await _db.Friendships.AsNoTracking().Where(f => f.UserId1 == request.UserId || f.UserId2 == request.UserId).ToListAsync(cancellationToken);
        if (friendships.Count == 0)
        {
            return Array.Empty<FriendDto>();
        }

        var friendids = friendships.Select(f => f.UserId1 == request.UserId ? f.UserId2 : f.UserId1).Distinct().ToList();
        var users = await _db.Users.AsNoTracking().Where(u => friendids.Contains(u.Id)).ToListAsync(cancellationToken);

        var ssnKey = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var snapshots = await _db.AthleteSeasonSnapshots.AsNoTracking().Where(s => friendids.Contains(s.UserId) && s.SeasonKey == ssnKey)
        .ToDictionaryAsync(s => s.UserId, cancellationToken);

        return users.Select(u =>
        {
            snapshots.TryGetValue(u.Id, out var snap);
            var isOptedIn = u.GlobalLeaderboardOptIn;
            var tier = isOptedIn ? (snap?.Tier ?? "Bronze") : "Unranked";

            return new FriendDto(
            Id: u.Id,
            Name: u.DisplayName,
            Initials: FriendshipHelpers.extractInitials(u.DisplayName),
            AvatarUrl: u.ProfileImageUrl,
            Code: u.FriendCode,
            DotsScore: snap?.DotsScore ?? 0m,
            Tier: tier
            );
        }).ToList();
    }
}