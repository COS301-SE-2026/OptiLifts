using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class ToggleLeaderboardOptInHandler : IRequestHandler<ToggleLeaderboardOptInCommand, ToggleLeaderboardOptInResult>
{
    private readonly OptiLiftsDbContext _db;

    public ToggleLeaderboardOptInHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<ToggleLeaderboardOptInResult> Handle(ToggleLeaderboardOptInCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return new ToggleLeaderboardOptInResult(false, false);
        }

        user.GlobalLeaderboardOptIn = request.OptIn;
    
        var ssnKey = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var snap = await _db.AthleteSeasonSnapshots.FirstOrDefaultAsync(s => s.UserId == request.UserId && s.SeasonKey == ssnKey, cancellationToken);

        if (snap is not null)
        {
            snap.IsOptedIn = request.OptIn;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ToggleLeaderboardOptInResult(true, request.OptIn);
    }
}
