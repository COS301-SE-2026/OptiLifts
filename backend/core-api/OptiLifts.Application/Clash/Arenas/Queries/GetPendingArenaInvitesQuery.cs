using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Queries;

public sealed record GetPendingArenaInvitesQuery(
    Guid UserId = default
) : IRequest<IReadOnlyList<ArenaInviteDto>>
{
    public Guid UserId { get; init; } = UserId;
}
