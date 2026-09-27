using MediatR;

namespace OptiLifts.Application.Clash.Friends.Queries;

public sealed record GetFriendsListQuery(
    Guid UserId
) : IRequest<IReadOnlyList<FriendDto>>;