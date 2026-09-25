using MediatR;

namespace OptiLifts.Application.Clash.Duels.Commands;

public sealed record SendDuelHypeCommand(Guid UserId, Guid DuelId) : IRequest<SendDuelHypeResult>;

public sealed record SendDuelHypeResult(bool Success, string Message);
