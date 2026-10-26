using MediatR;

namespace OptiLifts.Application.Clash.Friends.Queries;

public sealed record GetFriendRequestPrivacyQuery(Guid UserId) : IRequest<string>;
