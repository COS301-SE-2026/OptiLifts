using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Commands;

public sealed record LeaveArenaResult(bool Success, string Message, bool IsOwner = false)
{
    public static implicit operator bool(LeaveArenaResult result) => result.Success;
}

public sealed record LeaveArenaCommand(
    Guid ArenaId,
    Guid UserId = default
) : IRequest<LeaveArenaResult>
{
    public Guid UserId { get; init; } = UserId;
    public string? ArenaStringId { get; init; }

    public LeaveArenaCommand(string arenaId, Guid userId = default)
        : this(Guid.TryParse(arenaId, out var g) ? g : Guid.Empty, userId)
    {
        ArenaStringId = arenaId;
    }
}
