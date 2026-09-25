using MediatR;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Application.Clash.Notifications;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class WorkoutCompletedNotificationHandler : INotificationHandler<WorkoutCompletedNotification>
{
    private readonly ISender _sender;

    public WorkoutCompletedNotificationHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task Handle(WorkoutCompletedNotification notification, CancellationToken cancellationToken)
    {
        return _sender.Send(new RecalculateAthleteSeasonSnapshotCommand(notification.UserId), cancellationToken);
    }
}
