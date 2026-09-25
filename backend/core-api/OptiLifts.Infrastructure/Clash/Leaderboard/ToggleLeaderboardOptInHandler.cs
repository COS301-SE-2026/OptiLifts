using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class ToggleLeaderboardOptInHandler : IRequestHandler<ToggleLeaderboardOptInCommand, ToggleLeaderboardOptInResult>
{
    private readonly OptiLiftsDbContext _db;
    private readonly ISender _sender;

    public ToggleLeaderboardOptInHandler(OptiLiftsDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<ToggleLeaderboardOptInResult> Handle(ToggleLeaderboardOptInCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return new ToggleLeaderboardOptInResult(false, false, "User not found");
        }
        if (request.OptIn)
        {
            if (string.IsNullOrWhiteSpace(user.Weight) ||!float.TryParse(user.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var bodyweightKg) || bodyweightKg <= 0f)
            {
                return new ToggleLeaderboardOptInResult(false, false, "Please set your bodyweight in your profile before opting into competitive leaderboards.");
            }
        }

        user.GlobalLeaderboardOptIn = request.OptIn;

        var ssnKey = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var snap = await _db.AthleteSeasonSnapshots.FirstOrDefaultAsync(s => s.UserId == request.UserId && s.SeasonKey == ssnKey, cancellationToken);

        if (snap is not null)
        {
            snap.IsOptedIn = request.OptIn;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (request.OptIn)
        {
            await _sender.Send(new RecalculateAthleteSeasonSnapshotCommand(request.UserId), cancellationToken);
        }

        return new ToggleLeaderboardOptInResult(true, request.OptIn);
    }
}
