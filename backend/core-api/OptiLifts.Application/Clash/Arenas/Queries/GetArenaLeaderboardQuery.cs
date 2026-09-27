using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Queries;

public sealed record GetArenaLeaderboardQuery(
    Guid ArenaId,
    Guid UserId = default
) : IRequest<ArenaLeaderboardResult?>
{
    public Guid UserId { get; init; } = UserId;
    public string? ArenaStringId { get; init; }

    public GetArenaLeaderboardQuery(string arenaId, Guid userId = default)
        : this(Guid.TryParse(arenaId, out var g) ? g : Guid.Empty, userId)
    {
        ArenaStringId = arenaId;
    }
}
