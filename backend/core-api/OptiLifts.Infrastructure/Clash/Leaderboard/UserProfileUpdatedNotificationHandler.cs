using MediatR;
using OptiLifts.Application.Clash.Leaderboard.Commands;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class UserProfileUpdatedNotificationHandler : IRequestHandler<UserProfileUpdatedCommand>
{
    private readonly ISender _sender;

    public UserProfileUpdatedNotificationHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task Handle(UserProfileUpdatedCommand request, CancellationToken cancellationToken)
    {
        return _sender.Send(new RecalculateAthleteSeasonSnapshotCommand(request.UserId), cancellationToken);
    }
}
