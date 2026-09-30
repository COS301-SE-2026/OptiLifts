using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Infrastructure.Database.Seeders;

// OptiClash data between the demo athletes (friends, a private arena, duels, plus one
// pending friend request and one pending duel for Alex). Sends the same MediatR commands
// the API endpoints use, so DOTS snapshots, duel baselines, timelines and the arena feed
// are all built by the app's own handlers. Runs on every startup, so anything that
// already exists is skipped.
public static class ClashDemoSeeder
{
    private const string AlexEmail = "gymgoer@gmail.com";
    private const string ArenaName = "Demo Squad";
    private const string PendingRequesterEmail = "demo2@optilifts.com";

    // must be exactly "Deadlift": the all-compound duel only counts Barbell Back Squat,
    // Barbell Bench Press and Deadlift (see DuelTelemetrySetLoggedNotificationHandler)
    private const string DuelExerciseName = "Deadlift";
    private const string CompoundDuelName = "All Compound Lifts Combined";

    private static readonly string[] AthleteEmails =
    [
        AlexEmail,
        "maya@optilifts.com",
        "liam@optilifts.com",
        "zoe@optilifts.com"
    ];

    private sealed record DemoDuel(string Challenger, string Rival, bool AllCompound, string TargetType, int Days, bool Accept);

    private static readonly DemoDuel[] DemoDuels =
    [
        new(AlexEmail, "liam@optilifts.com", false, "TotalVolumeKg", 7, true),
        new("maya@optilifts.com", "zoe@optilifts.com", false, "E1RMGainPercent", 14, true),
        new(AlexEmail, "maya@optilifts.com", true, "TotalVolumeKg", 7, true),
        new("zoe@optilifts.com", AlexEmail, false, "E1RMGainPercent", 30, false)
    ];

    // deadlift sets each athlete logs once new duels start: (weight, reps)
    private static readonly Dictionary<string, (float Weight, int Reps)[]> DuelSessions = new()
    {
        [AlexEmail] = [(100f, 5), (110f, 5), (120f, 3)],
        ["maya@optilifts.com"] = [(70f, 5), (75f, 5), (80f, 3)],
        ["liam@optilifts.com"] = [(140f, 5), (150f, 5), (160f, 3)],
        ["zoe@optilifts.com"] = [(80f, 5), (85f, 5), (92.5f, 3)]
    };

    public static async Task SeedAsync(OptiLiftsDbContext dbContext, ISender sender, CancellationToken cancellationToken = default)
    {
        var ids = new Dictionary<string, Guid>();
        foreach (var email in AthleteEmails)
        {
            var hash = EmailHasher.HashEmail(email);
            var id = await dbContext.Users.Where(u => u.EmailHash == hash).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(cancellationToken);
            if (id is null)
            {
                return;
            }
            ids[email] = id.Value;
        }

        await SeedFriendshipsAsync(dbContext, sender, ids, cancellationToken);
        await SeedPendingFriendRequestAsync(dbContext, sender, ids[AlexEmail], cancellationToken);
        await SeedOptInsAsync(dbContext, sender, ids, cancellationToken);
        await SeedArenaAsync(dbContext, sender, ids, cancellationToken);

        var duelsStarted = await SeedDuelsAsync(dbContext, sender, ids, cancellationToken);

        // only log when duels were just started, otherwise every restart would add more
        // volume to the duel scores (they are stored on the duel, not recalculated)
        if (duelsStarted > 0)
        {
            await LogDuelSessionsAsync(dbContext, sender, ids, cancellationToken);
        }
    }

    private static async Task SeedFriendshipsAsync(OptiLiftsDbContext dbContext, ISender sender, Dictionary<string, Guid> ids, CancellationToken cancellationToken)
    {
        for (var i = 0; i < AthleteEmails.Length; i++)
        {
            for (var j = i + 1; j < AthleteEmails.Length; j++)
            {
                var senderId = ids[AthleteEmails[i]];
                var receiverId = ids[AthleteEmails[j]];

                if (await AreFriendsAsync(dbContext, senderId, receiverId, cancellationToken))
                {
                    continue;
                }

                var pending = await dbContext.FriendRequests.AsNoTracking()
                    .Where(r => r.Status == FriendshipHelpers.statusPending &&
                        ((r.SenderId == senderId && r.ReceiverId == receiverId) || (r.SenderId == receiverId && r.ReceiverId == senderId)))
                    .FirstOrDefaultAsync(cancellationToken);

                if (pending is not null)
                {
                    await sender.Send(new RespondToFriendRequestCommand(pending.ReceiverId, pending.Id, true), cancellationToken);
                    continue;
                }

                var sent = await sender.Send(new SendFriendRequestCommand(senderId, TargetUserId: receiverId), cancellationToken);
                if (sent.Success && sent.RequestID is Guid requestId)
                {
                    await sender.Send(new RespondToFriendRequestCommand(receiverId, requestId, true), cancellationToken);
                }
            }
        }
    }

    private static async Task SeedPendingFriendRequestAsync(OptiLiftsDbContext dbContext, ISender sender, Guid alexId, CancellationToken cancellationToken)
    {
        var hash = EmailHasher.HashEmail(PendingRequesterEmail);
        var requesterId = await dbContext.Users.Where(u => u.EmailHash == hash).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(cancellationToken);
        if (requesterId is null || await AreFriendsAsync(dbContext, requesterId.Value, alexId, cancellationToken))
        {
            return;
        }

        var alreadyPending = await dbContext.FriendRequests.AsNoTracking()
            .AnyAsync(r => r.Status == FriendshipHelpers.statusPending &&
                ((r.SenderId == requesterId && r.ReceiverId == alexId) || (r.SenderId == alexId && r.ReceiverId == requesterId)), cancellationToken);

        if (!alreadyPending)
        {
            await sender.Send(new SendFriendRequestCommand(requesterId.Value, TargetUserId: alexId), cancellationToken);
        }
    }

    private static async Task SeedOptInsAsync(OptiLiftsDbContext dbContext, ISender sender, Dictionary<string, Guid> ids, CancellationToken cancellationToken)
    {
        var userIds = ids.Values.ToList();
        var notOptedIn = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && !u.GlobalLeaderboardOptIn)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in notOptedIn)
        {
            await sender.Send(new ToggleLeaderboardOptInCommand(userId, true), cancellationToken);
        }
    }

    private static async Task SeedArenaAsync(OptiLiftsDbContext dbContext, ISender sender, Dictionary<string, Guid> ids, CancellationToken cancellationToken)
    {
        var alexId = ids[AlexEmail];

        // a new arena is created once the previous one's season has ended
        var existing = await dbContext.Arenas.AsNoTracking()
            .Where(a => a.CreatedById == alexId && a.Name == ArenaName && a.Code != null && a.SeasonEndDate > DateTime.UtcNow)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        string arenaId;
        string arenaCode;
        if (existing is not null)
        {
            arenaId = existing.Id;
            arenaCode = existing.Code!;
        }
        else
        {
            var created = await sender.Send(new CreateArenaCommand(ArenaName, "DotsOverall", 30, alexId), cancellationToken);
            if (!created.Success || created.Arena?.Code is null)
            {
                return;
            }
            arenaId = created.Arena.Id;
            arenaCode = created.Arena.Code;
        }

        var memberIds = await dbContext.ArenaMembers.AsNoTracking()
            .Where(m => m.ArenaId == arenaId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in ids.Values.Where(id => id != alexId && !memberIds.Contains(id)))
        {
            await sender.Send(new JoinArenaByCodeCommand(arenaCode, userId), cancellationToken);
        }
    }

    private static async Task<int> SeedDuelsAsync(OptiLiftsDbContext dbContext, ISender sender, Dictionary<string, Guid> ids, CancellationToken cancellationToken)
    {
        var deadliftId = await GetDeadliftIdAsync(dbContext, cancellationToken);
        if (deadliftId is null)
        {
            return 0;
        }

        var started = 0;
        foreach (var duel in DemoDuels)
        {
            var challengerId = ids[duel.Challenger];
            var rivalId = ids[duel.Rival];
            var exerciseName = duel.AllCompound ? CompoundDuelName : DuelExerciseName;
            var wantedStatus = duel.Accept ? "Active" : "Pending";

            // finished or declined duels don't count, so a fresh one replaces them
            var exists = await dbContext.Duels.AsNoTracking().AnyAsync(d =>
                d.ChallengerUserId == challengerId && d.RivalUserId == rivalId &&
                d.ExerciseName == exerciseName && d.TargetType == duel.TargetType &&
                d.DurationDays == duel.Days && d.Status == wantedStatus, cancellationToken);

            if (exists)
            {
                continue;
            }

            var created = await sender.Send(new CreateDuelChallengeCommand(
                challengerId, rivalId, exerciseName, duel.AllCompound ? null : deadliftId, duel.TargetType, duel.Days), cancellationToken);

            if (!created.Success || created.DuelId is not Guid duelId || !duel.Accept)
            {
                continue;
            }

            if (await sender.Send(new RespondToDuelCommand(rivalId, duelId, true), cancellationToken))
            {
                started++;
            }
        }

        return started;
    }

    private static async Task LogDuelSessionsAsync(OptiLiftsDbContext dbContext, ISender sender, Dictionary<string, Guid> ids, CancellationToken cancellationToken)
    {
        var deadliftId = await GetDeadliftIdAsync(dbContext, cancellationToken);
        if (deadliftId is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var (email, userId) in ids)
        {
            var legsId = await dbContext.Workouts.AsNoTracking()
                .Where(w => w.CreatedBy == userId && w.Name == "Legs" && !w.IsDeleted)
                .Select(w => (Guid?)w.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (legsId is null)
            {
                continue;
            }

            var sets = DuelSessions[email]
                .Select((s, idx) => new CreateWorkoutLogSetReq(null, "Normal", s.Reps, s.Weight, null, null, 120, 8f, idx + 1, 0))
                .ToList();

            await sender.Send(new CreateWorkoutLogCom(
                userId, legsId.Value, Guid.NewGuid(), null, "OptiClash demo duel session", now.AddMinutes(-45), now,
                [new CreateWorkoutLogExerciseReq(deadliftId.Value, null, 1, 0, sets)]), cancellationToken);
        }
    }

    private static Task<Guid?> GetDeadliftIdAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.Exercises.AsNoTracking()
            .Where(e => e.Name == DuelExerciseName && e.UserId == null && !e.IsDeleted)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static Task<bool> AreFriendsAsync(OptiLiftsDbContext dbContext, Guid a, Guid b, CancellationToken cancellationToken)
    {
        var (userId1, userId2) = FriendshipHelpers.toCanonOrder(a, b);
        return dbContext.Friendships.AsNoTracking().AnyAsync(f => f.UserId1 == userId1 && f.UserId2 == userId2, cancellationToken);
    }
}
