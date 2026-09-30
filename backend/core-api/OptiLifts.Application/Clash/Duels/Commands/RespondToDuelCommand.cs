using MediatR;

namespace OptiLifts.Application.Clash.Duels.Commands;

public sealed record RespondToDuelCommand(Guid UserId, Guid DuelId, bool Accept) : IRequest<bool>;
