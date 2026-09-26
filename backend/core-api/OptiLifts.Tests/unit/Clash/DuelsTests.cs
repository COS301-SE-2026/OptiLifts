using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Application.Clash.Duels.Queries;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Notifications;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Clash.Duels;
using OptiLifts.Infrastructure.Database;
using Xunit;

namespace OptiLifts.Tests.Unit.Clash;

public sealed class DuelsTests : IDisposable
{
    private const string code1 = "ABC123";
    private const string code2 = "BCD123";
    private const string code3 = "CDE123";
    private const string passwordHash = "test-hash";

    private readonly SqliteConnection _connection;
    private readonly OptiLiftsDbContext _db;

    public DuelsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(_connection).Options;
        _db = new OptiLiftsDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<User> SeedUserAsync(string name, string code, string privacy = "Friends")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{code.ToLowerInvariant()}@example.com",
            EmailHash = $"hash_{code.ToLowerInvariant()}",
            PasswordHash = passwordHash,
            DisplayName = name,
            FriendCode = code,
            DuelInvitePrivacy = privacy
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task MakeFriendsAsync(Guid u1, Guid u2)
    {
        var (a, b) = FriendshipHelpers.toCanonOrder(u1, u2);
        _db.Friendships.Add(new Friendship { UserId1 = a, UserId2 = b, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
    }

    private async Task<Duel> SeedDuelAsync(
        Guid challengerId, Guid rivalId, string targetType,
        string status = "Active", DateTime? endDate = null,
        decimal? challengerBaseline = null, decimal? rivalBaseline = null,
        decimal challengerCurrent = 0m, decimal rivalCurrent = 0m,
        Guid? exerciseId = null, string exerciseName = "Barbell Back Squat")
    {
        var duel = new Duel
        {
            Title = "Test Duel",
            ChallengerUserId = challengerId,
            RivalUserId = rivalId,
            ExerciseId = exerciseId,
            ExerciseName = exerciseName,
            TargetType = targetType,
            DurationDays = 7,
            Status = status,
            StartDate = status == "Pending" ? null : DateTime.UtcNow.AddDays(-1),
            EndDate = endDate,
            ChallengerBaselineValue = challengerBaseline,
            RivalBaselineValue = rivalBaseline,
            ChallengerCurrentValue = challengerCurrent,
            RivalCurrentValue = rivalCurrent
        };
        _db.Duels.Add(duel);
        await _db.SaveChangesAsync();
        return duel;
    }

    private async Task<Guid> SeedMuscleAsync(string name)
    {
        var muscle = new Muscle { Id = Guid.NewGuid(), Name = name };
        _db.Muscles.Add(muscle);
        await _db.SaveChangesAsync();
        return muscle.Id;
    }

    private async Task<Guid> SeedExerAsync(string name, Guid muscleId)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = name,
            Mechanic = "compound",
            ExerciseType = ExerciseType.WeightReps,
            PrimaryMuscleId = muscleId
        };
        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync();
        return exercise.Id;
    }

    private async Task<Guid> SeedSetAsync(Guid userId, Guid exerciseId, float weight, int reps)
    {
        var completedAt = DateTime.UtcNow;
        var workout = new Workout { Id = Guid.NewGuid(), Name = "Test Workout", CreatedBy = userId };
        _db.Workouts.Add(workout);

        var entry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = userId,
            Scheduled = completedAt,
            Status = ScheduleStatus.Completed
        };
        _db.ScheduledEntries.Add(entry);

        var log = new WorkoutLog
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            StartedAt = completedAt,
            CompletedAt = completedAt
        };
        _db.WorkoutLogs.Add(log);

        _db.WorkoutLogSets.Add(new WorkoutSetLog
        {
            Id = Guid.NewGuid(),
            LogId = log.Id,
            ExerciseId = exerciseId,
            Type = SetType.Normal,
            Reps = reps,
            Weight = weight
        });

        await _db.SaveChangesAsync();
        return log.Id;
    }

    [Fact]
    public void ResolveFalseIfPending()
    {
        var duel = new Duel { Status = "Pending", EndDate = DateTime.UtcNow.AddDays(-1) };
        DuelResolutionEngine.TryResolve(duel, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void ResolveFalseIfNotEnded()
    {
        var duel = new Duel { Status = "Active", EndDate = DateTime.UtcNow.AddDays(1) };
        DuelResolutionEngine.TryResolve(duel, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void ResolveChallengerWinsHigherVol()
    {
        var duel = new Duel
        {
            Status = "Active",
            EndDate = DateTime.UtcNow.AddMinutes(-1),
            TargetType = "TotalVolumeKg",
            ChallengerUserId = Guid.NewGuid(),
            RivalUserId = Guid.NewGuid(),
            ChallengerCurrentValue = 500m,
            RivalCurrentValue = 300m
        };

        var resolved = DuelResolutionEngine.TryResolve(duel, DateTime.UtcNow);

        resolved.Should().BeTrue();
        duel.Status.Should().Be("Finished");
        duel.WinnerUserId.Should().Be(duel.ChallengerUserId);
        duel.IsDraw.Should().BeFalse();
    }

    [Fact]
    public void ResolveDrawEqualVols()
    {
        var duel = new Duel
        {
            Status = "Active",
            EndDate = DateTime.UtcNow.AddMinutes(-1),
            TargetType = "TotalVolumeKg",
            ChallengerCurrentValue = 400m,
            RivalCurrentValue = 400m
        };

        DuelResolutionEngine.TryResolve(duel, DateTime.UtcNow);

        duel.IsDraw.Should().BeTrue();
        duel.WinnerUserId.Should().BeNull();
    }

    [Fact]
    public void ResolvePctGainCompare()
    {
        var duel = new Duel
        {
            Status = "Active",
            EndDate = DateTime.UtcNow.AddMinutes(-1),
            TargetType = "E1RMGainPercent",
            ChallengerUserId = Guid.NewGuid(),
            RivalUserId = Guid.NewGuid(),
            ChallengerBaselineValue = 100m,
            ChallengerCurrentValue = 110m,
            RivalBaselineValue = 100m,
            RivalCurrentValue = 104m
        };

        DuelResolutionEngine.TryResolve(duel, DateTime.UtcNow);

        duel.WinnerUserId.Should().Be(duel.ChallengerUserId);
    }

    [Fact]
    public void ResolveNullBaselineZero()
    {
        var duel = new Duel
        {
            Status = "Active",
            EndDate = DateTime.UtcNow.AddMinutes(-1),
            TargetType = "E1RMGainPercent",
            ChallengerUserId = Guid.NewGuid(),
            RivalUserId = Guid.NewGuid(),
            ChallengerBaselineValue = null,
            ChallengerCurrentValue = 0m,
            RivalBaselineValue = 100m,
            RivalCurrentValue = 105m
        };

        DuelResolutionEngine.TryResolve(duel, DateTime.UtcNow);

        duel.WinnerUserId.Should().Be(duel.RivalUserId);
    }

    [Fact]
    public async Task CreateDuelFailsSelf()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new CreateDuelChallengeHandler(_db);

        var res = await handler.Handle(new CreateDuelChallengeCommand(user.Id, user.Id, "Barbell Back Squat", null, "TotalVolumeKg", 7), CancellationToken.None);

        res.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDuelFailsNotFriends()
    {
        var user = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var handler = new CreateDuelChallengeHandler(_db);

        var res = await handler.Handle(new CreateDuelChallengeCommand(user.Id, rival.Id, "Barbell Back Squat", null, "TotalVolumeKg", 7), CancellationToken.None);

        res.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDuelFailsBadDuration()
    {
        var user = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        await MakeFriendsAsync(user.Id, rival.Id);
        var handler = new CreateDuelChallengeHandler(_db);

        var res = await handler.Handle(new CreateDuelChallengeCommand(user.Id, rival.Id, "Barbell Back Squat", null, "TotalVolumeKg", 10), CancellationToken.None);

        res.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDuelFailsRivalPrivacy()
    {
        var user = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2, privacy: "None");
        await MakeFriendsAsync(user.Id, rival.Id);
        var handler = new CreateDuelChallengeHandler(_db);

        var res = await handler.Handle(new CreateDuelChallengeCommand(user.Id, rival.Id, "Barbell Back Squat", null, "TotalVolumeKg", 7), CancellationToken.None);

        res.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDuelVolBaselineZero()
    {
        var user = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        await MakeFriendsAsync(user.Id, rival.Id);
        var handler = new CreateDuelChallengeHandler(_db);

        var res = await handler.Handle(new CreateDuelChallengeCommand(user.Id, rival.Id, "Barbell Back Squat", null, "TotalVolumeKg", 7), CancellationToken.None);

        res.Success.Should().BeTrue();
        var duel = await _db.Duels.FirstAsync(d => d.Id == res.DuelId);
        duel.Status.Should().Be("Pending");
        duel.ChallengerBaselineValue.Should().Be(0m);
    }

    [Fact]
    public async Task CreateDuelE1RMBaselineNull()
    {
        var user = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        await MakeFriendsAsync(user.Id, rival.Id);
        var handler = new CreateDuelChallengeHandler(_db);

        var res = await handler.Handle(new CreateDuelChallengeCommand(user.Id, rival.Id, "Barbell Back Squat", null, "E1RMGainPercent", 7), CancellationToken.None);

        var duel = await _db.Duels.FirstAsync(d => d.Id == res.DuelId);
        duel.ChallengerBaselineValue.Should().BeNull();
    }

    [Fact]
    public async Task RespondAcceptActivates()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", status: "Pending");

        var handler = new RespondToDuelHandler(_db);
        var ok = await handler.Handle(new RespondToDuelCommand(rival.Id, duel.Id, true), CancellationToken.None);

        ok.Should().BeTrue();
        var upd = await _db.Duels.FirstAsync(d => d.Id == duel.Id);
        upd.Status.Should().Be("Active");
        upd.EndDate.Should().NotBeNull();
    }

    [Fact]
    public async Task RespondDeclineSetsDeclined()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", status: "Pending");

        var handler = new RespondToDuelHandler(_db);
        await handler.Handle(new RespondToDuelCommand(rival.Id, duel.Id, false), CancellationToken.None);

        var upd = await _db.Duels.FirstAsync(d => d.Id == duel.Id);
        upd.Status.Should().Be("Declined");
    }

    [Fact]
    public async Task RespondFalseIfNotRival()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", status: "Pending");

        var handler = new RespondToDuelHandler(_db);
        var ok = await handler.Handle(new RespondToDuelCommand(challenger.Id, duel.Id, true), CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task HypeFailsNotParticipant()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var outsider = await SeedUserAsync("Cersei", code3);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg");

        var handler = new SendDuelHypeHandler(_db);
        var res = await handler.Handle(new SendDuelHypeCommand(outsider.Id, duel.Id), CancellationToken.None);

        res.Success.Should().BeFalse();
    }

    [Fact]
    public async Task HypeSucceedsParticipant()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg");

        var handler = new SendDuelHypeHandler(_db);
        var res = await handler.Handle(new SendDuelHypeCommand(rival.Id, duel.Id), CancellationToken.None);

        res.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UserDuelsResolvesExpired()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg",
            status: "Active", endDate: DateTime.UtcNow.AddDays(-1),
            challengerCurrent: 500m, rivalCurrent: 300m);

        var handler = new GetUserDuelsHandler(_db);
        var res = await handler.Handle(new GetUserDuelsQuery(challenger.Id, "All"), CancellationToken.None);

        var upd = await _db.Duels.FirstAsync(d => d.Id == duel.Id);
        upd.Status.Should().Be("Finished");
        upd.WinnerUserId.Should().Be(challenger.Id);
        res.Won.Should().Be(1);
    }

    [Fact]
    public async Task UserDuelsExcludesPendingDeclined()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", status: "Pending");
        await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", status: "Declined");

        var handler = new GetUserDuelsHandler(_db);
        var res = await handler.Handle(new GetUserDuelsQuery(challenger.Id, "All"), CancellationToken.None);

        res.Duels.Should().BeEmpty();
    }

    [Fact]
    public async Task DuelDetailNullIfNotParticipant()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var outsider = await SeedUserAsync("Cersei", code3);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg");

        var handler = new GetDuelDetailHandler(_db);
        var res = await handler.Handle(new GetDuelDetailQuery(outsider.Id, duel.Id), CancellationToken.None);

        res.Should().BeNull();
    }

    [Fact]
    public async Task DuelDetailTimelineNewestFirst()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg");

        _db.DuelTimelineEvents.Add(new DuelTimelineEvent { DuelId = duel.Id, UserId = challenger.Id, EventText = "First", CreatedAt = DateTime.UtcNow.AddMinutes(-5) });
        _db.DuelTimelineEvents.Add(new DuelTimelineEvent { DuelId = duel.Id, UserId = challenger.Id, EventText = "Second", CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var handler = new GetDuelDetailHandler(_db);
        var res = await handler.Handle(new GetDuelDetailQuery(challenger.Id, duel.Id), CancellationToken.None);

        res!.Timeline.Should().HaveCount(2);
        res.Timeline[0].EventText.Should().Be("Second");
    }

    [Fact]
    public async Task InvitesEmptyIfNone()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new GetPendingDuelInvitesHandler(_db);

        var res = await handler.Handle(new GetPendingDuelInvitesQuery(user.Id), CancellationToken.None);

        res.Should().BeEmpty();
    }

    [Fact]
    public async Task InvitesReturnsForRival()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", status: "Pending");

        var handler = new GetPendingDuelInvitesHandler(_db);
        var res = await handler.Handle(new GetPendingDuelInvitesQuery(rival.Id), CancellationToken.None);

        res.Should().HaveCount(1);
        res[0].ChallengerName.Should().Be("Dany");
    }

    [Fact]
    public async Task TelemetryAddsVol()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var muscleId = await SeedMuscleAsync("Quadriceps");
        var squatId = await SeedExerAsync("Barbell Back Squat", muscleId);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", exerciseId: squatId);
        var logId = await SeedSetAsync(challenger.Id, squatId, 100f, 5);

        var handler = new DuelTelemetrySetLoggedNotificationHandler(_db);
        await handler.Handle(new WorkoutCompletedNotification(challenger.Id, logId), CancellationToken.None);

        var upd = await _db.Duels.FirstAsync(d => d.Id == duel.Id);
        upd.ChallengerCurrentValue.Should().Be(500m);
        var evtCount = await _db.DuelTimelineEvents.CountAsync(e => e.DuelId == duel.Id);
        evtCount.Should().Be(1);
    }

    [Fact]
    public async Task TelemetryCalibratesBaseline()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var muscleId = await SeedMuscleAsync("Quadriceps");
        var squatId = await SeedExerAsync("Barbell Back Squat", muscleId);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "E1RMGainPercent", exerciseId: squatId);
        var logId = await SeedSetAsync(challenger.Id, squatId, 100f, 5);

        var handler = new DuelTelemetrySetLoggedNotificationHandler(_db);
        await handler.Handle(new WorkoutCompletedNotification(challenger.Id, logId), CancellationToken.None);

        var upd = await _db.Duels.FirstAsync(d => d.Id == duel.Id);
        upd.ChallengerBaselineValue.Should().NotBeNull();
        upd.ChallengerCurrentValue.Should().Be(upd.ChallengerBaselineValue!.Value);
    }

    [Fact]
    public async Task TelemetryIgnoresUnrelatedExer()
    {
        var challenger = await SeedUserAsync("Dany", code1);
        var rival = await SeedUserAsync("Drogo", code2);
        var muscleId = await SeedMuscleAsync("Quadriceps");
        var squatId = await SeedExerAsync("Barbell Back Squat", muscleId);
        var benchId = await SeedExerAsync("Barbell Bench Press", muscleId);
        var duel = await SeedDuelAsync(challenger.Id, rival.Id, "TotalVolumeKg", exerciseId: squatId);
        var logId = await SeedSetAsync(challenger.Id, benchId, 100f, 5);

        var handler = new DuelTelemetrySetLoggedNotificationHandler(_db);
        await handler.Handle(new WorkoutCompletedNotification(challenger.Id, logId), CancellationToken.None);

        var upd = await _db.Duels.FirstAsync(d => d.Id == duel.Id);
        upd.ChallengerCurrentValue.Should().Be(0m);
    }

    [Fact]
    public async Task TelemetryNoActiveDuelsNoop()
    {
        var user = await SeedUserAsync("Dany", code1);
        var muscleId = await SeedMuscleAsync("Quadriceps");
        var squatId = await SeedExerAsync("Barbell Back Squat", muscleId);
        var logId = await SeedSetAsync(user.Id, squatId, 100f, 5);

        var handler = new DuelTelemetrySetLoggedNotificationHandler(_db);
        var act = async () => await handler.Handle(new WorkoutCompletedNotification(user.Id, logId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
