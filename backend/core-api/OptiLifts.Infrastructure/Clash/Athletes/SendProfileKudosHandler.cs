using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Athletes.Commands;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Athletes;

public sealed class SendProfileKudosHandler : IRequestHandler<SendProfileKudosCommand, SendProfileKudosResult>
{
    private readonly OptiLiftsDbContext _db;

    public SendProfileKudosHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<SendProfileKudosResult> Handle(SendProfileKudosCommand request, CancellationToken cancellationToken)
    {
        if (request.TargetUserId == request.SenderUserId)
        {
            return new SendProfileKudosResult(false, "You cannot send kudos to yourself");
        }

        var targetExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == request.TargetUserId, cancellationToken);
        if (!targetExists)
        {
            return new SendProfileKudosResult(false, "Athlete not found");
        }

        var alreadySent = await _db.AthleteProfileKudos.AsNoTracking()
            .AnyAsync(k => k.TargetUserId == request.TargetUserId && k.SenderUserId == request.SenderUserId, cancellationToken);
        if (alreadySent)
        {
            return new SendProfileKudosResult(false, "You have already sent kudos to this athlete");
        }

        _db.AthleteProfileKudos.Add(new AthleteProfileKudos
        {
            TargetUserId = request.TargetUserId,
            SenderUserId = request.SenderUserId
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new SendProfileKudosResult(true, "Kudos sent");
    }
}
