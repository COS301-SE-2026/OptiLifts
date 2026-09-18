using MediatR;

namespace OptiLifts.Application.Clash.Friends.Commands;

public sealed record RejectAllFriendRequestsCommand(
    Guid UserId
) : IRequest<int>;