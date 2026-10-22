using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Commands;

public sealed record RemoveActivityKudosCommand(
    Guid ActivityId,
    Guid UserId = default
) : IRequest<RemoveActivityKudosResult>
{
    public Guid UserId { get; init; } = UserId;
}

public sealed record RemoveActivityKudosResult(
    bool Success,
    int KudosCount,
    string Message
);
