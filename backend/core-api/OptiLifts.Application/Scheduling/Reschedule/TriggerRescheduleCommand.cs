using MediatR;

namespace OptiLifts.Application.Scheduling.Reschedule;

public record TriggerRescheduleCommand(Guid UserId, List<Guid> SelectedMissedEntryIds) : IRequest<RescheduleResultDto>;
