using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Commands;

public sealed record InviteFriendToArenaCommand(
    Guid ArenaId,
    Guid FriendId,
    Guid UserId = default
) : IRequest<InviteFriendToArenaResult>
{
    public Guid UserId { get; init; } = UserId;
    public string? ArenaStringId { get; init; }

    public InviteFriendToArenaCommand(string arenaId, Guid friendId, Guid userId = default)
        : this(Guid.TryParse(arenaId, out var g) ? g : Guid.Empty, friendId, userId)
    {
        ArenaStringId = arenaId;
    }
}
