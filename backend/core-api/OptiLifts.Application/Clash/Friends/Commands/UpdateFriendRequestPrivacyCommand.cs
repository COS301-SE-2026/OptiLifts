using MediatR;

namespace OptiLifts.Application.Clash.Friends.Commands;

public sealed record UpdateFriendRequestPrivacyCommand(Guid UserId, string Privacy) : IRequest<bool>;
