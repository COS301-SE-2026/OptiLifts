using MediatR;

namespace OptiLifts.Application.Clash.Friends.Commands;

public sealed record SendFriendRequestCommand(
    Guid UserId,
    string FriendCode
) : IRequest<SendFriendRequestResult>;
public sealed record SendFriendRequestResult(
    bool Success,
    string Message,
    Guid? RequestID = null
);