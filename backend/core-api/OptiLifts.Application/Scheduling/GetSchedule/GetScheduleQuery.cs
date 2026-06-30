using MediatR;
using OptiLifts.Domain.Workouts;
namespace OptiLifts.Application.Scheduling.GetSchedule;

public sealed record GetScheduleQuery(
    Guid UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    ScheduleStatus? Status = null
) : IRequest<IReadOnlyList<ScheduledEntryDto>>;