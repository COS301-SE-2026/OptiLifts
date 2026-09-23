using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Queries;

public sealed record GetArenaFeedQuery(
    Guid ArenaId,
    Guid UserId = default
) : IRequest<IReadOnlyList<ClashActivityDto>>
{
    public Guid UserId { get; init; } = UserId;
    public string? ArenaStringId { get; init; }

    public GetArenaFeedQuery(string arenaId, Guid userId = default)
        : this(Guid.TryParse(arenaId, out var g) ? g : Guid.Empty, userId)
    {
        ArenaStringId = arenaId;
    }
}
