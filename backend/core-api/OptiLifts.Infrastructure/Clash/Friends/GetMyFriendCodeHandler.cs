using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Application.Clash.Friends.Queries;

namespace OptiLifts.Infrastructure.Clash.Friends;

public sealed class GetMyFriendCodeHandler : IRequestHandler<GetMyFriendCodeQuery, string?>{
    private readonly OptiLiftsDbContext _db;

    public GetMyFriendCodeHandler(OptiLiftsDbContext db){
        _db = db;
    }

    public async Task<string?> Handle(GetMyFriendCodeQuery request, CancellationToken cancellationToken){
        return await _db.Users.AsNoTracking().Where(u => u.Id == request.UserId)
        .Select(u => u.FriendCode).FirstOrDefaultAsync(cancellationToken);
    }
}