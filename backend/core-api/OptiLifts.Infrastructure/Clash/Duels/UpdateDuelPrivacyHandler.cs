using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class UpdateDuelPrivacyHandler : IRequestHandler<UpdateDuelPrivacyCommand, bool>
{
    private readonly OptiLiftsDbContext _db;

    public UpdateDuelPrivacyHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateDuelPrivacyCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }
        var normalised = request.Privacy.Equals("none", StringComparison.OrdinalIgnoreCase) ? "None" : "Friends";
        user.DuelInvitePrivacy = normalised;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
