using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class GetDuelDetailHandler : IRequestHandler<GetDuelDetailQuery, DuelDetailDto?>
{
    private readonly OptiLiftsDbContext _db;

    public GetDuelDetailHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<DuelDetailDto?> Handle(GetDuelDetailQuery request, CancellationToken cancellationToken)
    {
        var duel = await _db.Duels.FirstOrDefaultAsync(
            d => d.Id == request.DuelId && (d.ChallengerUserId == request.UserId || d.RivalUserId == request.UserId), cancellationToken);

        if (duel is null)
        {
            return null;
        }

        if (DuelResolutionEngine.TryResolve(duel, DateTime.UtcNow))
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        var challenger = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == duel.ChallengerUserId, cancellationToken);
        var rival = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == duel.RivalUserId, cancellationToken);

        var timelineRows = await _db.DuelTimelineEvents
            .AsNoTracking()
            .Where(e => e.DuelId == duel.Id)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var timelineUserIds = timelineRows.Select(e => e.UserId).Distinct().ToArray();
        var timelineUsers = await _db.Users.AsNoTracking().Where(u => timelineUserIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var timeline = timelineRows
            .Select(e => new DuelTimelineEventDto(
                e.Id,
                e.UserId,
                timelineUsers.TryGetValue(e.UserId, out var eventUser) ? eventUser.DisplayName : "Unknown",
                e.EventText,
                e.IsPr,
                e.CreatedAt))
            .ToList();

        return new DuelDetailDto(
            duel.Id,
            duel.Title,
            duel.ChallengerUserId,
            challenger?.DisplayName ?? "Unknown",
            challenger?.ProfileImageUrl,
            duel.RivalUserId,
            rival?.DisplayName ?? "Unknown",
            rival?.ProfileImageUrl,
            duel.ExerciseName,
            duel.TargetType,
            duel.Status,
            duel.StartDate,
            duel.EndDate,
            duel.ChallengerCurrentValue,
            duel.RivalCurrentValue,
            duel.ChallengerBaselineValue,
            duel.RivalBaselineValue,
            duel.WinnerUserId,
            duel.IsDraw,
            timeline
        );
    }
}
