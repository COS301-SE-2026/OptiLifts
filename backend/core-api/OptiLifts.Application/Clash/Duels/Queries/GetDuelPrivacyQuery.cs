using MediatR;

namespace OptiLifts.Application.Clash.Duels.Queries;

public sealed record GetDuelPrivacyQuery(Guid UserId) : IRequest<string>;
