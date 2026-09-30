using MediatR;

namespace OptiLifts.Application.Clash.Duels.Commands;

public sealed record UpdateDuelPrivacyCommand(Guid UserId, string Privacy) : IRequest<bool>;
