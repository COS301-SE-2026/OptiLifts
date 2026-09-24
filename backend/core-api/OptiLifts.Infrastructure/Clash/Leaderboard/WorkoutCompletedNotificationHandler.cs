using MediatR;
using OptiLifts.Application.Clash.Leaderboard.Commands;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class WorkoutCompletedNotificationHandler : IRequestHandler<WorkoutCompletedCommand>
{
    private readonly ISender _sender;

    public WorkoutCompletedNotificationHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task Handle(WorkoutCompletedCommand request, CancellationToken cancellationToken)
    {
        return _sender.Send(new RecalculateAthleteSeasonSnapshotCommand(request.UserId), cancellationToken);
    }
}
