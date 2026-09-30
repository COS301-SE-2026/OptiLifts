using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class GetUserDuelsHandler : IRequestHandler<GetUserDuelsQuery, UserDuelsResult>
{
    private readonly OptiLiftsDbContext _db;

    public GetUserDuelsHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<UserDuelsResult> Handle(GetUserDuelsQuery request, CancellationToken cancellationToken)
    {
        var duels = await _db.Duels
            .Where(d => d.ChallengerUserId == request.UserId || d.RivalUserId == request.UserId)
            .Where(d => d.Status != "Pending" && d.Status != "Declined")
            .ToListAsync(cancellationToken);

        var anyResolved = false;
        var currTime = DateTime.UtcNow;
        foreach (var duel in duels)
        {
            if (DuelResolutionEngine.TryResolve(duel, currTime))
            {
                anyResolved = true;
            }
        }

        if (anyResolved)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        var filtered = string.IsNullOrWhiteSpace(request.FilterStatus) || request.FilterStatus == "All"
            ? duels : duels.Where(d => d.Status == request.FilterStatus).ToList();

        var userIds = filtered.SelectMany(d => new[] { d.ChallengerUserId, d.RivalUserId }).Distinct().ToArray();
        var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        var dtos = filtered.OrderByDescending(d => d.CreatedAt).Select(d => ToDto(d, users)).ToList();

        var wonDuels = duels.Count(d => d.WinnerUserId == request.UserId);
        var lostDuels = duels.Count(d => d.Status == "Finished" && !d.IsDraw && d.WinnerUserId != request.UserId);
        var activeDuels = duels.Count(d => d.Status == "Active");
        var finishedCount = duels.Count(d => d.Status == "Finished");
        var winRate = finishedCount > 0 ? Math.Round((decimal)wonDuels / finishedCount * 100m, 1) : 0m;

        return new UserDuelsResult(dtos, wonDuels, lostDuels, activeDuels, winRate);
    }

    private static DuelSummaryDto ToDto(Duel d, Dictionary<Guid, User> users)
    {
        users.TryGetValue(d.ChallengerUserId, out var challenger);
        users.TryGetValue(d.RivalUserId, out var rival);

        return new DuelSummaryDto(
            d.Id,
            d.Title,
            d.ChallengerUserId,
            challenger?.DisplayName ?? "Unknown",
            challenger?.ProfileImageUrl,
            d.RivalUserId,
            rival?.DisplayName ?? "Unknown",
            rival?.ProfileImageUrl,
            d.ExerciseName,
            d.TargetType,
            d.Status,
            d.StartDate,
            d.EndDate,
            d.ChallengerCurrentValue,
            d.RivalCurrentValue,
            d.ChallengerBaselineValue,
            d.RivalBaselineValue,
            d.WinnerUserId,
            d.IsDraw
        );
    }
}
