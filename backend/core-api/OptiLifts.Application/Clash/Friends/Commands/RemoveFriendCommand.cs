using MediatR;

namespace OptiLifts.Application.Clash.Friends.Commands;

public sealed record RemoveFriendCommand(
    Guid UserId,
    Guid FriendId
) : IRequest<bool>;