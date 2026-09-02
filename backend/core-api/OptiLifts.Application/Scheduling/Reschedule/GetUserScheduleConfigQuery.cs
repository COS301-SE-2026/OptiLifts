using MediatR;

namespace OptiLifts.Application.Scheduling.Reschedule;

public record GetUserScheduleConfigQuery(Guid UserId) : IRequest<UserScheduleConfigDto>;
