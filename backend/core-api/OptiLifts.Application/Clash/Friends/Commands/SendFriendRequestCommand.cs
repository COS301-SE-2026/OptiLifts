using MediatR;

namespace OptiLifts.Application.Clash.Friends.Commands;

public sealed record SendFriendRequestCommand(
    Guid UserId,
    string? FriendCode = null,
    Guid? TargetUserId = null
) : IRequest<SendFriendRequestResult>;
public sealed record SendFriendRequestResult(
    bool Success,
    string Message,
    Guid? RequestID = null
);