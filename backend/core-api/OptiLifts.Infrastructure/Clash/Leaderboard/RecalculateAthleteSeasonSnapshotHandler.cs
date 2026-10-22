using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Leaderboard.Commands;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Leaderboard;

public sealed class RecalculateAthleteSeasonSnapshotHandler : IRequestHandler<RecalculateAthleteSeasonSnapshotCommand>
{
    private const string SquatExerciseName = "Barbell Back Squat";
    private const string BenchExerciseName = "Barbell bench press";
    private const string DeadliftExerciseName = "Barbell deadlift";

    private readonly OptiLiftsDbContext _dbContext;

    public RecalculateAthleteSeasonSnapshotHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RecalculateAthleteSeasonSnapshotCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return;
        }

        if (!float.TryParse(user.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var bodyweightKg) || bodyweightKg <= 0f)
        {
            return;
        }

        var gender = string.Equals(user.Sex, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";

        var now = DateTime.UtcNow;
        //ssn = season
        var ssnStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var ssnEnd = ssnStart.AddMonths(1);
        var ssnKey = ssnStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        var exerIdByLift = await GetCompoundExerIdsAsync(cancellationToken);

        var sqt1RM = await GetPeakE1RMAsync(request.UserId, exerIdByLift[SquatExerciseName], ssnStart, ssnEnd, cancellationToken);
        var bch1RM = await GetPeakE1RMAsync(request.UserId, exerIdByLift[BenchExerciseName], ssnStart, ssnEnd, cancellationToken);
        var dlft1RM = await GetPeakE1RMAsync(request.UserId, exerIdByLift[DeadliftExerciseName], ssnStart, ssnEnd, cancellationToken);
        var totalE1RM = sqt1RM + bch1RM + dlft1RM;

        var dotsScore = (decimal)DotsCalculationEngine.CalculateDots((float)totalE1RM, bodyweightKg, gender);

        var (tier, tierLevel) = DetermineTier(dotsScore);

        var weeklyVolInKgs = await GetWeeklyVolAsync(request.UserId, ssnStart, ssnEnd, cancellationToken);
        var lastWorkoutDate = await GetLastWorkoutDateAsync(request.UserId, cancellationToken) ?? ssnStart.Date;

        //making of snapshot
        var snap = await _dbContext.AthleteSeasonSnapshots.FirstOrDefaultAsync(s => s.UserId == request.UserId && s.SeasonKey == ssnKey, cancellationToken);

        if (snap is null)
        {
            snap = new AthleteSeasonSnapshot
            {
                UserId = request.UserId,
                SeasonKey = ssnKey
            };

            _dbContext.AthleteSeasonSnapshots.Add(snap);
        }

        snap.DisplayName = user.DisplayName;
        snap.AvatarUrl = user.ProfileImageUrl;
        snap.Gender = gender;
        snap.BodyweightKg = (decimal)bodyweightKg;
        snap.IsOptedIn = user.GlobalLeaderboardOptIn;
        snap.Squat1RM = sqt1RM;
        snap.Bench1RM = bch1RM;
        snap.Deadlift1RM = dlft1RM;
        snap.TotalE1RM = totalE1RM;
        snap.DotsScore = dotsScore;
        snap.Tier = tier;
        snap.TierLevel = tierLevel;
        snap.WeeklyVolumeKg = weeklyVolInKgs;
        snap.LastWorkoutDate = lastWorkoutDate;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Guid[]>> GetCompoundExerIdsAsync(CancellationToken cancellationToken)
    {
        var squat = SquatExerciseName.ToLowerInvariant();
        var bench = BenchExerciseName.ToLowerInvariant();
        var deadlift = DeadliftExerciseName.ToLowerInvariant();

        var exer = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.UserId == null &&
                (e.Name.ToLower() == squat || e.Name.ToLower() == bench || e.Name.ToLower() == deadlift))
            .Select(e => new { e.Id, e.Name })
            .ToListAsync(cancellationToken);

        return new Dictionary<string, Guid[]>
        {
            [SquatExerciseName] = exer.Where(e => string.Equals(e.Name, SquatExerciseName, StringComparison.OrdinalIgnoreCase)).Select(e => e.Id).ToArray(),
            [BenchExerciseName] = exer.Where(e => string.Equals(e.Name, BenchExerciseName, StringComparison.OrdinalIgnoreCase)).Select(e => e.Id).ToArray(),
            [DeadliftExerciseName] = exer.Where(e => string.Equals(e.Name, DeadliftExerciseName, StringComparison.OrdinalIgnoreCase)).Select(e => e.Id).ToArray()
        };
    }

    private async Task<decimal> GetPeakE1RMAsync(Guid userId, Guid[] exerciseIds, DateTime seasonStart, DateTime seasonEnd, CancellationToken cancellationToken)
    {
        if (exerciseIds.Length == 0)
        {
            return 0m;
        }

        var sets = await _dbContext.WorkoutLogSets
            .AsNoTracking()
            .Join(_dbContext.WorkoutLogs, s => s.LogId, l => l.Id, (s, l) => new { Set = s, Log = l })
            .Join(_dbContext.ScheduledEntries, x => x.Log.EntryId, e => e.Id, (x, e) => new { x.Set, x.Log, Entry = e })
            .Where(x => x.Entry.UserId == userId
                && exerciseIds.Contains(x.Set.ExerciseId)
                && x.Set.Type == SetType.Normal
                && x.Log.CompletedAt != null
                && x.Log.CompletedAt >= seasonStart
                && x.Log.CompletedAt < seasonEnd)
            .Select(x => new { x.Set.Weight, x.Set.Reps })
            .ToListAsync(cancellationToken);

        if (sets.Count == 0)
        {
            return 0m;
        }

        var best = sets.Max(s => E1RMCalculationEngine.CalculateE1RM(s.Weight, s.Reps));
        return (decimal)best;
    }

    private async Task<decimal> GetWeeklyVolAsync(Guid userId, DateTime seasonStart, DateTime seasonEnd, CancellationToken cancellationToken)
    {
        var sets = await _dbContext.WorkoutLogSets
            .AsNoTracking()
            .Join(_dbContext.WorkoutLogs, s => s.LogId, l => l.Id, (s, l) => new { Set = s, Log = l })
            .Join(_dbContext.ScheduledEntries, x => x.Log.EntryId, e => e.Id, (x, e) => new { x.Set, x.Log, Entry = e })
            .Where(x => x.Entry.UserId == userId
                && x.Set.Type == SetType.Normal
                && x.Log.CompletedAt != null
                && x.Log.CompletedAt >= seasonStart
                && x.Log.CompletedAt <= seasonEnd)
            .Select(x => new { x.Set.Weight, x.Set.Reps })
            .ToListAsync(cancellationToken);

        return (decimal)sets.Sum(s => s.Weight * s.Reps);
    }

    private async Task<DateTime?> GetLastWorkoutDateAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkoutLogs
            .AsNoTracking()
            .Join(_dbContext.ScheduledEntries, l => l.EntryId, e => e.Id, (l, e) => new { Log = l, Entry = e })
            .Where(x => x.Entry.UserId == userId && x.Log.CompletedAt != null)
            .OrderByDescending(x => x.Log.CompletedAt)
            .Select(x => (DateTime?)x.Log.CompletedAt!.Value.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static (string Tier, int TierLevel) DetermineTier(decimal dotsScore) => dotsScore switch
    {
        >= 465.0m => ("Overload Master", 1),
        >= 414.0m => ("Diamond", 1),
        >= 366.0m => ("Diamond", 2),
        >= 320.0m => ("Diamond", 3),
        >= 276.0m => ("Gold", 1),
        >= 235.0m => ("Gold", 2),
        >= 196.0m => ("Gold", 3),
        >= 159.0m => ("Silver", 1),
        >= 123.0m => ("Silver", 2),
        >= 90.0m => ("Silver", 3),
        >= 58.0m => ("Bronze", 1),
        >= 28.0m => ("Bronze", 2),
        _ => ("Bronze", 3)
    };
}
