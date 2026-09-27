using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Queries;

public sealed record GetUserArenasQuery(
    Guid UserId = default
) : IRequest<IReadOnlyList<ArenaDto>>
{
    public Guid UserId { get; init; } = UserId;
}
