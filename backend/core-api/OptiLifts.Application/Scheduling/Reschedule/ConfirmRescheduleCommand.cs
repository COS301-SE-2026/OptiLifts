using MediatR;

namespace OptiLifts.Application.Scheduling.Reschedule;

public record ConfirmRescheduleCommand(Guid UserId, List<ConfirmRescheduleItemDto> Items) : IRequest<bool>;