using MediatR;

namespace OptiLifts.Application.Scheduling.Reschedule;

public record UpdateUserScheduleConfigCommand(Guid UserId, UserScheduleConfigDto Config) : IRequest<UserScheduleConfigDto>;