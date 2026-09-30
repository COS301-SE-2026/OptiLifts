using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class GetPendingFriendRequestsHandler : IRequestHandler<GetPendingFriendRequestsQuery, PendingFriendRequestsResult>
{
    private readonly OptiLiftsDbContext _db;

    public GetPendingFriendRequestsHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<PendingFriendRequestsResult> Handle(GetPendingFriendRequestsQuery request, CancellationToken cancellationToken)
    {
        var incoming = await _db.FriendRequests.AsNoTracking()
        .Where(r => r.ReceiverId == request.UserId && r.Status == FriendshipHelpers.statusPending)
        .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

        var outgoing = await _db.FriendRequests.AsNoTracking()
        .Where(r => r.SenderId == request.UserId && r.Status == FriendshipHelpers.statusPending)
        .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

        var relatedUsers = incoming.Select(r => r.SenderId).Concat(outgoing.Select(r => r.ReceiverId)).Distinct().ToList();
        var users = await _db.Users.AsNoTracking().Where(u => relatedUsers.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var incoReq = incoming.Select(r =>
        {
            users.TryGetValue(r.SenderId, out var sender);
            var name = sender?.DisplayName ?? "Athlete";
            return new FriendRequestDto(
                Id: r.Id,
                FromAthleteId: r.SenderId,
                FromName: name,
                FromInitials: FriendshipHelpers.extractInitials(name),
                FromAvatarUrl: sender?.ProfileImageUrl,
                FromCode: sender?.FriendCode ?? string.Empty,
                SentAt: r.CreatedAt
            );
        }).ToList();

        var outReq = outgoing.Select(r =>
        {
            users.TryGetValue(r.ReceiverId, out var receiver);
            var name = receiver?.DisplayName ?? "Athlete";
            return new FriendRequestDto(
                Id: r.Id,
                FromAthleteId: r.ReceiverId,
                FromName: name,
                FromInitials: FriendshipHelpers.extractInitials(name),
                FromAvatarUrl: receiver?.ProfileImageUrl,
                FromCode: receiver?.FriendCode ?? string.Empty,
                SentAt: r.CreatedAt
            );
        }).ToList();

        return new PendingFriendRequestsResult(incoReq, outReq);
    }
}