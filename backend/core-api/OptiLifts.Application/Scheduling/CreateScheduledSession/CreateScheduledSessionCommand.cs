using MediatR;
using OptiLifts.Domain.Workouts;
namespace OptiLifts.Application.Scheduling.CreateScheduledSession;

public sealed record CreateScheduledSessionCommand(
    Guid UserId,
    Guid WorkoutId,
    DateTime ScheduledAt,
    ScheduleStatus? Status,
    string? Repeat = null,
    int? Interval = null,
    DateTime? Until = null
) : IRequest<CreateScheduledSessionResult?>;

public sealed record CreateScheduledSessionResult(
    Guid Id,
    Guid WorkoutId,
    DateTime ScheduledAt,
    ScheduleStatus Status
);