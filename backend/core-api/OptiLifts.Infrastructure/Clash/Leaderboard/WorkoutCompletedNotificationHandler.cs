using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Clash.Arenas;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class WorkoutCompletedNotificationHandler : IRequestHandler<WorkoutCompletedCommand>
{
    private readonly ISender _sender;
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public WorkoutCompletedNotificationHandler(ISender sender, OptiLiftsDbContext db, IClashNotifier notifer)
    {
        _sender = sender;
        _db = db;
        _notifier = notifer;
    }

    public async Task Handle(WorkoutCompletedCommand request, CancellationToken cancellationToken)
    {
        await _sender.Send(new RecalculateAthleteSeasonSnapshotCommand(request.UserId), cancellationToken);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return;

        var memberships = await _db.ArenaMembers.AsNoTracking().Where(m => m.UserId == request.UserId).ToListAsync(cancellationToken);
        if (memberships.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var mem in memberships)
        {
            var activity = new ClashActivity
            {
                Id = Guid.NewGuid(),
                ArenaId = mem.ArenaId,
                UserId = request.UserId,
                EventText = "Completed a Workout",
                Details = "Logged a workout session and updated leaderboard rankings",
                IsPr = false,
                IsPromotion = false,
                KudosCount = 0,
                CreatedAt = now
            };
            _db.ClashActivities.Add(activity);
            await _db.SaveChangesAsync(cancellationToken);
            await ClashFeedHelper.PruneArenaActivitiesAsync(_db, mem.ArenaId, cancellationToken);

            var activityDto = new ClashActivityDto(
                Id: activity.Id,
                ArenaId: mem.ArenaId,
                UserId: user.Id,
                UserName: user.DisplayName,
                UserInitials: FriendshipHelpers.extractInitials(user.DisplayName),
                UserAvatarUrl: user.ProfileImageUrl,
                EventText: activity.EventText,
                Details: activity.Details,
                IsPr: activity.IsPr,
                IsPromotion: activity.IsPromotion,
                KudosCount: activity.KudosCount,
                HasUserKudoed: false,
                CreatedAt: activity.CreatedAt
            );
            await _notifier.BroadcastActivityAsync(mem.ArenaId, activityDto, cancellationToken);
        }

    }
}
