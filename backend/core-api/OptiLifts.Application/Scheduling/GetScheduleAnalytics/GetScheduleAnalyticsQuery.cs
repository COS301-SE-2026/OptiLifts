using MediatR;
namespace OptiLifts.Application.Scheduling.GetScheduleAnalytics;

public sealed record GetScheduleAnalyticsQuery(
    Guid UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<ScheduleAnalyticsDto>;