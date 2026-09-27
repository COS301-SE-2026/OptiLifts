using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Notifications;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class ArenaActivityOnWorkoutCompletedHandler : INotificationHandler<WorkoutCompletedNotification>
{
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public ArenaActivityOnWorkoutCompletedHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task Handle(WorkoutCompletedNotification notification, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var memberships = await _db.ArenaMembers.AsNoTracking().Where(m => m.UserId == notification.UserId).ToListAsync(cancellationToken);
        if (memberships.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var mem in memberships)
        {
            var activity = new ClashActivity
            {
                Id = Guid.NewGuid(),
                ArenaId = mem.ArenaId,
                UserId = notification.UserId,
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
