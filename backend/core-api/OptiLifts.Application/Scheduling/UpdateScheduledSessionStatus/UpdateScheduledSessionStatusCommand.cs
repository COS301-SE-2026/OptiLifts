using MediatR;
using OptiLifts.Domain.Workouts;
namespace OptiLifts.Application.Scheduling.UpdateScheduledSessionStatus;

public sealed record UpdateScheduledSessionStatusCommand(
    Guid UserId,
    Guid SessionId,
    ScheduleStatus Status
) : IRequest<UpdateScheduledSessionStatusResult?>;

public sealed record UpdateScheduledSessionStatusResult(
    Guid Id,
    Guid WorkoutId,
    DateTime ScheduledAt,
    ScheduleStatus Status
);