using MediatR;

namespace OptiLifts.Application.Clash.Notifications;

public sealed record UserProfileUpdatedNotification(Guid UserId) : INotification;
