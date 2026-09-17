using MediatR;

namespace OptiLifts.Application.Clash.Friends.Queries;

public sealed record GetMyFriendCodeQuery(Guid UserId) : IRequest<string?>;