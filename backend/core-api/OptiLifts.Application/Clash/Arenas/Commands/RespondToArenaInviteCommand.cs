using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Commands;

public sealed record RespondToArenaInviteCommand(
    Guid InviteId,
    bool Accept,
    Guid UserId = default
) : IRequest<bool>
{
    public Guid UserId { get; init; } = UserId;
}
