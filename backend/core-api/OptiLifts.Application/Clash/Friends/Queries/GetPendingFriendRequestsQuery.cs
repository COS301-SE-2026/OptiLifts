using MediatR;

namespace OptiLifts.Application.Clash.Friends.Queries;

public sealed record GetPendingFriendRequestsQuery(
    Guid UserId
): IRequest<PendingFriendRequestsResult>;