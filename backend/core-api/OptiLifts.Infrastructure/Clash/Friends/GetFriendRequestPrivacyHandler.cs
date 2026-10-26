using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends.Queries;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class GetFriendRequestPrivacyHandler : IRequestHandler<GetFriendRequestPrivacyQuery, string>
{
    private readonly OptiLiftsDbContext _db;

    public GetFriendRequestPrivacyHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<string> Handle(GetFriendRequestPrivacyQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        return user?.FriendRequestPrivacy ?? "Everyone";
    }
}
