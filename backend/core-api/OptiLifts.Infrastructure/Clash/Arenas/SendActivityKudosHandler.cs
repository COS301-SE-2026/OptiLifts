using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class SendActivityKudosHandler : IRequestHandler<SendActivityKudosCommand, SendActivityKudosResult>
{
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public SendActivityKudosHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task<SendActivityKudosResult> Handle(SendActivityKudosCommand request, CancellationToken cancellationToken)
    {
        var activity = await _db.ClashActivities
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity is null)
        {
            return new SendActivityKudosResult(false, 0, "Activity not found.");
        }

        var alreadyKudoed = await _db.ClashActivityKudos
            .AnyAsync(k => k.ActivityId == activity.Id && k.SenderUserId == request.UserId, cancellationToken);

        if (alreadyKudoed)
        {
            return new SendActivityKudosResult(false, activity.KudosCount, "You have already cheered this activity.");
        }

        var kudos = new ClashActivityKudos
        {
            Id = Guid.NewGuid(),
            ActivityId = activity.Id,
            SenderUserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        activity.KudosCount += 1;
        _db.ClashActivityKudos.Add(kudos);

        await _db.SaveChangesAsync(cancellationToken);

        await _notifier.BroadcastKudosAsync(activity.ArenaId, activity.Id, activity.KudosCount, cancellationToken);

        return new SendActivityKudosResult(true, activity.KudosCount, "Cheer sent successfully!");
    }
}
