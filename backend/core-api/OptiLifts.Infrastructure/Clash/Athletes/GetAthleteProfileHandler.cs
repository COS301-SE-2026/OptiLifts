using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Athletes.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Athletes;

public sealed class GetAthleteProfileHandler : IRequestHandler<GetAthleteProfileQuery, AthleteProfileResult?>
{
    private const int WorkoutCount = 5;
    private const int MuscBalanceWindowDays = 30;

    private readonly OptiLiftsDbContext _db;

    public GetAthleteProfileHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<AthleteProfileResult?> Handle(GetAthleteProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.AthleteId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var ssnKey = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var snap = await _db.AthleteSeasonSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == request.AthleteId && s.SeasonKey == ssnKey, cancellationToken);

        var trophies = await GetTrophiesAsync(request.AthleteId, cancellationToken);
        var recentWorkouts = await GetRecentWorkoutsAsync(request.AthleteId, cancellationToken);
        var muscBal = await GetMuscleBalanceAsync(request.AthleteId, cancellationToken);

        var amountOfKudos = await _db.AthleteProfileKudos.AsNoTracking()
            .CountAsync(k => k.TargetUserId == request.AthleteId, cancellationToken);

        var hasSentKudos = await _db.AthleteProfileKudos.AsNoTracking()
            .AnyAsync(k => k.TargetUserId == request.AthleteId && k.SenderUserId == request.RequestingUserId, cancellationToken);

        var isFriend = await _db.Friendships.AsNoTracking().AnyAsync(f => (f.UserId1 == request.RequestingUserId && f.UserId2 == request.AthleteId)
        || (f.UserId1 == request.AthleteId && f.UserId2 == request.RequestingUserId), cancellationToken);

        return new AthleteProfileResult(
            user.Id,
            user.DisplayName,
            user.ProfileImageUrl,
            snap?.BodyweightKg ?? 0m,
            snap?.Tier ?? "Bronze",
            snap?.TierLevel ?? 3,
            snap?.DotsScore ?? 0m,
            snap?.Squat1RM ?? 0m,
            snap?.Bench1RM ?? 0m,
            snap?.Deadlift1RM ?? 0m,
            snap?.TotalE1RM ?? 0m,
            snap?.WeeklyVolumeKg ?? 0m,
            muscBal,
            trophies,
            recentWorkouts,
            amountOfKudos,
            hasSentKudos,
            isFriend
        );
    }

    private async Task<IReadOnlyList<MuscleBalanceDto>> GetMuscleBalanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.AddDays(-MuscBalanceWindowDays);

        var setsPrimaryMuscle = await _db.WorkoutLogSets
            .AsNoTracking()
            .Join(_db.WorkoutLogs, s => s.LogId, l => l.Id, (s, l) => new { Set = s, Log = l })
            .Join(_db.ScheduledEntries, x => x.Log.EntryId, e => e.Id, (x, e) => new { x.Set, x.Log, Entry = e })
            .Where(x => x.Entry.UserId == userId && x.Set.Type == SetType.Normal && x.Log.CompletedAt != null && x.Log.CompletedAt >= windowStart)
            .Join(_db.Exercises, x => x.Set.ExerciseId, ex => ex.Id, (x, ex) => new { x.Set, ex.PrimaryMuscleId })
            .Join(_db.Muscles, x => x.PrimaryMuscleId, m => m.Id, (x, m) => new { x.Set.Weight, x.Set.Reps, MuscleName = m.Name })
            .ToListAsync(cancellationToken);

        var totals = MuscleGroupMapper.RadarGroups.ToDictionary(group => group, _ => 0m);

        foreach (var row in setsPrimaryMuscle)
        {
            var muscleGroup = MuscleGroupMapper.GetRadarGroup(row.MuscleName);

            if (muscleGroup is null)
            {
                continue;
            }

            totals[muscleGroup] += (decimal)(row.Weight * row.Reps);
        }

        return MuscleGroupMapper.RadarGroups
            .Select(group => new MuscleBalanceDto(group, totals[group])).ToList();
    }

    private async Task<IReadOnlyList<TrophyDto>> GetTrophiesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await _db.UserBadges
            .AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .Join(_db.Badges, ub => ub.BadgeId, b => b.Id, (ub, b) => new
            {
                b.Id,
                b.Name,
                b.Category,
                b.Description,
                b.IconUrl,
                ub.EarnedAt
            })
            .OrderByDescending(x => x.EarnedAt)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new TrophyDto(r.Id, r.Name, r.Category.ToString(), r.Description, r.IconUrl, r.EarnedAt))
            .ToList();
    }

    private async Task<IReadOnlyList<RecentWorkoutDto>> GetRecentWorkoutsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var logs = await _db.WorkoutLogs
            .AsNoTracking()
            .Join(_db.ScheduledEntries, l => l.EntryId, e => e.Id, (l, e) => new { Log = l, Entry = e })
            .Where(x => x.Entry.UserId == userId && x.Log.CompletedAt != null)
            .OrderByDescending(x => x.Log.CompletedAt)
            .Take(WorkoutCount)
            .Join(_db.Workouts, x => x.Entry.WorkoutId, w => w.Id, (x, w) => new { x.Log, WorkoutName = w.Name })
            .ToListAsync(cancellationToken);

        var res = new List<RecentWorkoutDto>(logs.Count);

        foreach (var entry in logs)
        {
            var sets = await _db.WorkoutLogSets
                .AsNoTracking()
                .Where(s => s.LogId == entry.Log.Id && s.Type == SetType.Normal)
                .Join(_db.Exercises, s => s.ExerciseId, ex => ex.Id, (s, ex) => new { s.Weight, s.Reps, ExerciseName = ex.Name })
                .ToListAsync(cancellationToken);

            var volKg = (decimal)sets.Sum(s => s.Weight * s.Reps);

            var exer = sets
                .GroupBy(s => s.ExerciseName)
                .Select(g => new RecentWorkoutExerciseDto(g.Key, g.Count())).ToList();

            res.Add(new RecentWorkoutDto(
                entry.Log.Id,
                entry.WorkoutName,
                entry.Log.Notes,
                entry.Log.CompletedAt,
                volKg,
                exer));
        }

        return res;
    }
}
