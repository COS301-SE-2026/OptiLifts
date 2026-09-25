using MediatR;

namespace OptiLifts.Application.Clash.Notifications;

public sealed record WorkoutCompletedNotification(Guid UserId, Guid LogId) : INotification;
