using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class CreateDuelChallengeHandler : IRequestHandler<CreateDuelChallengeCommand, CreateDuelChallengeResult>
{
    private static readonly HashSet<int> AllowedDurations = new() { 7, 14, 30 };
    private static readonly HashSet<string> AllowedTargetTypes = new() { "E1RMGainPercent", "TotalVolumeKg" };

    private readonly OptiLiftsDbContext _db;

    public CreateDuelChallengeHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<CreateDuelChallengeResult> Handle(CreateDuelChallengeCommand request, CancellationToken cancellationToken)
    {
        if (request.ChallengerUserId == request.RivalUserId)
        {
            return new CreateDuelChallengeResult(false, "You cannot challenge yourself");
        }

        if (!AllowedDurations.Contains(request.DurationDays))
        {
            return new CreateDuelChallengeResult(false, "Duration must be 7, 14, or 30 days");
        }

        if (!AllowedTargetTypes.Contains(request.TargetType))
        {
            return new CreateDuelChallengeResult(false, "Invalid target type");
        }

        var rival = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.RivalUserId, cancellationToken);

        if (rival is null)
        {
            return new CreateDuelChallengeResult(false, "Athlete not found");
        }

        if (rival.DuelInvitePrivacy == "None")
        {
            return new CreateDuelChallengeResult(false, "This athlete is not accepting duel challenges");
        }

        var (u1, u2) = FriendshipHelpers.toCanonOrder(request.ChallengerUserId, request.RivalUserId);
        var isFriends = await _db.Friendships.AsNoTracking().AnyAsync(f => f.UserId1 == u1 && f.UserId2 == u2, cancellationToken);

        if (!isFriends)
        {
            return new CreateDuelChallengeResult(false, "You can only challenge your friends");
        }

        var initVal = request.TargetType == "TotalVolumeKg" ? 0m : (decimal?)null;

        var duel = new Duel
        {
            Title = $"{request.DurationDays}-Day {request.ExerciseName} Duel",
            ChallengerUserId = request.ChallengerUserId,
            RivalUserId = request.RivalUserId,
            ExerciseId = request.ExerciseId,
            ExerciseName = request.ExerciseName,
            TargetType = request.TargetType,
            DurationDays = request.DurationDays,
            Status = "Pending",
            ChallengerBaselineValue = initVal,
            RivalBaselineValue = initVal
        };

        _db.Duels.Add(duel);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateDuelChallengeResult(true, "Duel challenge sent", duel.Id);
    }
}
