using MediatR;

namespace OptiLifts.Application.Clash.Friends.Commands;

public sealed record RespondToFriendRequestCommand(
    Guid UserId,
    Guid RequestId,
    bool Accept
) : IRequest<bool>;