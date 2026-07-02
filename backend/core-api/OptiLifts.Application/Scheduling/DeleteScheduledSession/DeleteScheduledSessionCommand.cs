using MediatR;
using OptiLifts.Domain.Workouts;
namespace OptiLifts.Application.Scheduling.DeleteScheduledSession;

public sealed record DeleteScheduledSessionCommand(
    Guid UserId,
    Guid SessionId
) : IRequest<bool>;