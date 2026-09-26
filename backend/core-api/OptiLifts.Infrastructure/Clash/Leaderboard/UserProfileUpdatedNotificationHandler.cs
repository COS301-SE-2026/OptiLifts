using MediatR;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Application.Clash.Notifications;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class UserProfileUpdatedNotificationHandler : INotificationHandler<UserProfileUpdatedNotification>
{
    private readonly ISender _sender;

    public UserProfileUpdatedNotificationHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task Handle(UserProfileUpdatedNotification notification, CancellationToken cancellationToken)
    {
        return _sender.Send(new RecalculateAthleteSeasonSnapshotCommand(notification.UserId), cancellationToken);
    }
}
