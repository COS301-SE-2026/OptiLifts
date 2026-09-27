using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class RespondToDuelHandler : IRequestHandler<RespondToDuelCommand, bool>
{
    private readonly OptiLiftsDbContext _db;

    public RespondToDuelHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(RespondToDuelCommand request, CancellationToken cancellationToken)
    {
        var duel = await _db.Duels.FirstOrDefaultAsync(
            d => d.Id == request.DuelId && d.RivalUserId == request.UserId && d.Status == "Pending",
            cancellationToken);

        if (duel is null)
        {
            return false;
        }

        if (request.Accept)
        {
            duel.Status = "Active";
            duel.StartDate = DateTime.UtcNow;
            duel.EndDate = DateTime.UtcNow.AddDays(duel.DurationDays);
        }
        else
        {
            duel.Status = "Declined";
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
