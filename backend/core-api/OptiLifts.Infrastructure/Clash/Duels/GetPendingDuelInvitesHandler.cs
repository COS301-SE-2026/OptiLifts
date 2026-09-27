using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Queries;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class GetPendingDuelInvitesHandler : IRequestHandler<GetPendingDuelInvitesQuery, IReadOnlyList<DuelInviteDto>>
{
    private readonly OptiLiftsDbContext _db;

    public GetPendingDuelInvitesHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DuelInviteDto>> Handle(GetPendingDuelInvitesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _db.Duels
            .AsNoTracking()
            .Where(d => d.RivalUserId == request.UserId && d.Status == "Pending")
            .OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Array.Empty<DuelInviteDto>();
        }

        var challengerIds = rows.Select(d => d.ChallengerUserId).Distinct().ToArray();
        var challengers = await _db.Users.AsNoTracking().Where(u => challengerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        return rows
            .Select(d =>
            {
                challengers.TryGetValue(d.ChallengerUserId, out var challenger);
                return new DuelInviteDto(
                    d.Id,
                    d.ChallengerUserId,
                    challenger?.DisplayName ?? "Unknown",
                    challenger?.ProfileImageUrl,
                    d.ExerciseName,
                    d.TargetType,
                    d.DurationDays,
                    d.CreatedAt);
            }).ToList();
    }
}
