using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Commands;

public sealed record SendActivityKudosCommand(
    Guid ActivityId,
    Guid UserId = default
) : IRequest<SendActivityKudosResult>
{
    public Guid UserId { get; init; } = UserId;
}
