using MediatR;
using OptiLifts.Domain.Workouts;
namespace OptiLifts.Application.Scheduling.GetScheduleAnalytics;

public sealed record GetScheduleAnalyticsQuery(
    Guid UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    ScheduleStatus? Status = null
) : IRequest<ScheduleAnalyticsDto>;