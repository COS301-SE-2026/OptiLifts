using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class RemoveActivityKudosHandler : IRequestHandler<RemoveActivityKudosCommand, RemoveActivityKudosResult>
{
    private readonly OptiLiftsDbContext _db;

    public RemoveActivityKudosHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }
    public async Task<RemoveActivityKudosResult> Handle(RemoveActivityKudosCommand request, CancellationToken cancellationToken)
    {
        var activity = await _db.ClashActivities.FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);
        if (activity is null)
        {
            return new RemoveActivityKudosResult(false, 0, "Activity not found.");
        }

        var existingKudos = await _db.ClashActivityKudos.FirstOrDefaultAsync(k => k.ActivityId == activity.Id && k.SenderUserId == request.UserId, cancellationToken);
        if (existingKudos is null)
        {
            return new RemoveActivityKudosResult(false, activity.KudosCount, "You have not cheered this activity.");
        }

        activity.KudosCount = Math.Max(0, activity.KudosCount - 1);
        _db.ClashActivityKudos.Remove(existingKudos);
        await _db.SaveChangesAsync(cancellationToken);

        return new RemoveActivityKudosResult(true, activity.KudosCount, "Cheer removed successfully.");
    }
}
