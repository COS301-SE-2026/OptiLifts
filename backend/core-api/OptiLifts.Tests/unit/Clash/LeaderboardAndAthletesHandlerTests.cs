using System.Globalization;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Athletes.Commands;
using OptiLifts.Application.Clash.Athletes.Queries;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Application.Clash.Leaderboard.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Clash.Athletes;
using OptiLifts.Infrastructure.Clash.Leaderboard;
using OptiLifts.Infrastructure.Database;
using Xunit;

namespace OptiLifts.Tests.Unit.Clash;

public sealed class LeaderboardAndAthletesHandlerTests : IDisposable
{
    private const string code1 = "ABC123";
    private const string code2 = "BCD123";
    private const string code3 = "CDE123";
    private const string passwordHash = "test-hash";

    private readonly SqliteConnection _connection;
    private readonly OptiLiftsDbContext _db;

    public LeaderboardAndAthletesHandlerTests()
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

    private async Task<User> SeedUserAsync(string name, string code, string? weight = null, string? sex = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{code.ToLowerInvariant()}@example.com",
            EmailHash = $"hash_{code.ToLowerInvariant()}",
            PasswordHash = passwordHash,
            DisplayName = name,
            FriendCode = code,
            Weight = weight,
            Sex = sex
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task SeedSnapAsync(Guid userId, string seasonKey, decimal dots, string gender, decimal bodyweight, bool optedIn, DateTime lastWorkoutDate, string displayName = "Athlete")
    {
        _db.AthleteSeasonSnapshots.Add(new AthleteSeasonSnapshot
        {
            UserId = userId,
            SeasonKey = seasonKey,
            DisplayName = displayName,
            Gender = gender,
            BodyweightKg = bodyweight,
            IsOptedIn = optedIn,
            DotsScore = dots,
            Tier = "Bronze",
            LastWorkoutDate = lastWorkoutDate
        });
        await _db.SaveChangesAsync();
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

    private async Task SeedSetAsync(Guid userId, Guid exerciseId, float weight, int reps, DateTime completedAt)
    {
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
    }

    private static string SsnKey() => DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    [Fact]
    public async Task ToggleOptIn_Fails_WhenUserNotFound()
    {
        var handler = new ToggleLeaderboardOptInHandler(_db);
        var res = await handler.Handle(new ToggleLeaderboardOptInCommand(Guid.NewGuid(), true), CancellationToken.None);

        res.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleOptIn_UpdatesUserAndSnap_WhenOptingOut()
    {
        var user = await SeedUserAsync("Dany", code1);
        await SeedSnapAsync(user.Id, SsnKey(), 300m, "Female", 70m, optedIn: true, DateTime.UtcNow.Date);

        var handler = new ToggleLeaderboardOptInHandler(_db);
        var res = await handler.Handle(new ToggleLeaderboardOptInCommand(user.Id, false), CancellationToken.None);

        res.Success.Should().BeTrue();
        res.IsOptedIn.Should().BeFalse();

        var updUser = await _db.Users.FindAsync(user.Id);
        updUser!.GlobalLeaderboardOptIn.Should().BeFalse();

        var snap = await _db.AthleteSeasonSnapshots.FirstAsync(s => s.UserId == user.Id);
        snap.IsOptedIn.Should().BeFalse();
    }

    [Fact]
    public async Task SendKudos_Fails_WhenSendingToSelf()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new SendProfileKudosHandler(_db);
        var res = await handler.Handle(new SendProfileKudosCommand(user.Id, user.Id), CancellationToken.None);

        res.Success.Should().BeFalse();
        res.Message.Should().Be("You cannot send kudos to yourself");
    }

    [Fact]
    public async Task SendKudos_Fails_WhenTargetNotFound()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new SendProfileKudosHandler(_db);
        var res = await handler.Handle(new SendProfileKudosCommand(user.Id, Guid.NewGuid()), CancellationToken.None);

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Athlete not found");
    }

    [Fact]
    public async Task SendKudos_Fails_WhenAlreadySent()
    {
        var user = await SeedUserAsync("Dany", code1);
        var target = await SeedUserAsync("Drogo", code2);
        _db.AthleteProfileKudos.Add(new AthleteProfileKudos { TargetUserId = target.Id, SenderUserId = user.Id });
        await _db.SaveChangesAsync();

        var handler = new SendProfileKudosHandler(_db);
        var res = await handler.Handle(new SendProfileKudosCommand(user.Id, target.Id), CancellationToken.None);

        res.Success.Should().BeFalse();
        res.Message.Should().Be("You have already sent kudos to this athlete");
    }

    [Fact]
    public async Task SendKudos_Succeeds_AndPersistsKudos()
    {
        var user = await SeedUserAsync("Dany", code1);
        var target = await SeedUserAsync("Drogo", code2);

        var handler = new SendProfileKudosHandler(_db);
        var res = await handler.Handle(new SendProfileKudosCommand(user.Id, target.Id), CancellationToken.None);

        res.Success.Should().BeTrue();
        var saved = await _db.AthleteProfileKudos.CountAsync(k => k.TargetUserId == target.Id && k.SenderUserId == user.Id);
        saved.Should().Be(1);
    }

    [Fact]
    public async Task RecalcSnap_DoesNothing_WhenUserNotFound()
    {
        var handler = new RecalculateAthleteSeasonSnapshotHandler(_db);
        await handler.Handle(new RecalculateAthleteSeasonSnapshotCommand(Guid.NewGuid()), CancellationToken.None);

        var count = await _db.AthleteSeasonSnapshots.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task RecalcSnap_DoesNothing_WhenWeightUnparseable()
    {
        var user = await SeedUserAsync("Dany", code1, weight: null, sex: "Female");
        var handler = new RecalculateAthleteSeasonSnapshotHandler(_db);
        await handler.Handle(new RecalculateAthleteSeasonSnapshotCommand(user.Id), CancellationToken.None);

        var count = await _db.AthleteSeasonSnapshots.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task RecalcSnap_UpsertsSnap_WithCorrectSquatE1RM()
    {
        var user = await SeedUserAsync("Dany", code1, weight: "70", sex: "Female");
        var muscleId = await SeedMuscleAsync("Quadriceps");
        var squatId = await SeedExerAsync("Barbell Back Squat", muscleId);
        await SeedSetAsync(user.Id, squatId, 100f, 5, DateTime.UtcNow);

        var handler = new RecalculateAthleteSeasonSnapshotHandler(_db);
        await handler.Handle(new RecalculateAthleteSeasonSnapshotCommand(user.Id), CancellationToken.None);

        var snap = await _db.AthleteSeasonSnapshots.FirstOrDefaultAsync(s => s.UserId == user.Id);
        snap.Should().NotBeNull();
        snap!.Squat1RM.Should().BeApproximately(116.65m, 0.01m);
        snap.TotalE1RM.Should().Be(snap.Squat1RM);
        snap.Gender.Should().Be("Female");
        snap.DotsScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDivLeaderboard_ReturnsEmpty_ForUnknownBracket()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new GetDivisionalLeaderboardHandler(_db);
        var res = await handler.Handle(new GetDivisionalLeaderboardQuery(user.Id, "Female", "not-a-bracket", "DotsOverall", "monthly", 1, 10), CancellationToken.None);

        res.TotalCount.Should().Be(0);
        res.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProfile_ReturnsNull_WhenAthleteNotFound()
    {
        var handler = new GetAthleteProfileHandler(_db);
        var res = await handler.Handle(new GetAthleteProfileQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        res.Should().BeNull();
    }

    [Fact]
    public async Task GetProfile_ReturnsZeroedStats_WhenNoSnapExists()
    {
        var athlete = await SeedUserAsync("Dany", code1);
        var viewer = await SeedUserAsync("Drogo", code2);

        var handler = new GetAthleteProfileHandler(_db);
        var res = await handler.Handle(new GetAthleteProfileQuery(athlete.Id, viewer.Id), CancellationToken.None);

        res.Should().NotBeNull();
        res!.DotsScore.Should().Be(0m);
        res.Tier.Should().Be("Unranked");
        res.MuscleBalance30d.Should().HaveCount(6);
        res.Trophies.Should().BeEmpty();
        res.RecentWorkouts.Should().BeEmpty();
        res.KudosCount.Should().Be(0);
        res.HasSentKudos.Should().BeFalse();
    }

    [Fact]
    public async Task GetProfile_ReflectsKudosState()
    {
        var athlete = await SeedUserAsync("Dany", code1);
        var viewer = await SeedUserAsync("Drogo", code2);
        _db.AthleteProfileKudos.Add(new AthleteProfileKudos { TargetUserId = athlete.Id, SenderUserId = viewer.Id });
        await _db.SaveChangesAsync();

        var handler = new GetAthleteProfileHandler(_db);
        var res = await handler.Handle(new GetAthleteProfileQuery(athlete.Id, viewer.Id), CancellationToken.None);

        res!.KudosCount.Should().Be(1);
        res.HasSentKudos.Should().BeTrue();
    }
}
