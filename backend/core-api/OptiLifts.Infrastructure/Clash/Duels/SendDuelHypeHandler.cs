using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class SendDuelHypeHandler : IRequestHandler<SendDuelHypeCommand, SendDuelHypeResult>
{
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public SendDuelHypeHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task<SendDuelHypeResult> Handle(SendDuelHypeCommand request, CancellationToken cancellationToken)
    {
        var duel = await _db.Duels.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DuelId && (d.ChallengerUserId == request.UserId || d.RivalUserId == request.UserId), cancellationToken);

        if (duel is null)
        {
            return new SendDuelHypeResult(false, "Duel not found");
        }

        var sender = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (sender is null)
        {
            return new SendDuelHypeResult(false, "User not found");
        }

        await _notifier.BroadcastDuelHypeAsync(duel.Id, sender.DisplayName, cancellationToken);

        return new SendDuelHypeResult(true, "Hype sent");
    }
}
