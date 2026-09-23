using MediatR;

namespace OptiLifts.Application.Clash.Arenas.Commands;

public sealed record CreateArenaCommand(
    string Name,
    string MetricType,
    int DurationDays,
    Guid UserId = default
) : IRequest<CreateArenaResult>
{
    public Guid UserId { get; init; } = UserId;
}
