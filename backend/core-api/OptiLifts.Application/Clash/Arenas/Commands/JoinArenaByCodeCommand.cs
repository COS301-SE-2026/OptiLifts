using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Commands;

public sealed record JoinArenaByCodeCommand(
    string Code,
    Guid UserId = default
) : IRequest<JoinArenaResult>
{
    public Guid UserId { get; init; } = UserId;
}
