using MediatR;
namespace OptiLifts.Application.Scheduling.GetSchedule;

public sealed record GetScheduleQuery(
    Guid UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IReadOnlyList<ScheduledEntryDto>>;