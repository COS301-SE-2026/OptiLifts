using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Athletes.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Athletes;

public sealed class RemoveProfileKudosHandler : IRequestHandler<RemoveProfileKudosCommand, RemoveProfileKudosResult>
{
    private readonly OptiLiftsDbContext _db;

    public RemoveProfileKudosHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<RemoveProfileKudosResult> Handle(RemoveProfileKudosCommand request, CancellationToken cancellationToken)
    {
        if (request.TargetUserId == request.SenderUserId)
        {
            return new RemoveProfileKudosResult(false, "You cannot remove kudos from yourself");
        }

        var existingKudos = await _db.AthleteProfileKudos.FirstOrDefaultAsync(k => k.TargetUserId == request.TargetUserId && k.SenderUserId == request.SenderUserId, cancellationToken);
        if (existingKudos is null)
        {
            return new RemoveProfileKudosResult(false, "You have not sent kudos to this athlete");
        }

        _db.AthleteProfileKudos.Remove(existingKudos);
        await _db.SaveChangesAsync(cancellationToken);
        return new RemoveProfileKudosResult(true, "Kudos removed");
    }
}
