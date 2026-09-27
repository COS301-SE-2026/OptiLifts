using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Queries;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Duels;

public sealed class GetDuelPrivacyHandler : IRequestHandler<GetDuelPrivacyQuery, string>
{
    private readonly OptiLiftsDbContext _db;

    public GetDuelPrivacyHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<string> Handle(GetDuelPrivacyQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        return user?.DuelInvitePrivacy ?? "Friends";
    }
}
