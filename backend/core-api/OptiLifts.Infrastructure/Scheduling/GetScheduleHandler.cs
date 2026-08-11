using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class GetScheduleHandler : IRequestHandler<GetScheduleQuery, IReadOnlyList<ScheduledEntryDto>>
{
    private readonly OptiLiftsDbContext _dbContext;
    public GetScheduleHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ScheduledEntryDto>> Handle(GetScheduleQuery request, CancellationToken cancellationToken)
    {
        DateTime start, end;
        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            start = DateTime.SpecifyKind(request.StartDate.Value.Date, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(request.EndDate.Value.Date, DateTimeKind.Utc);
        }
        else
        {
            var now = DateTime.UtcNow.Date;
            int dif = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            start = DateTime.SpecifyKind(now.AddDays(-dif), DateTimeKind.Utc);
            end = DateTime.SpecifyKind(start.AddDays(6), DateTimeKind.Utc);
        }

        //add status filtering
        var query = _dbContext.ScheduledEntries.AsNoTracking()
            .Where(entry => entry.UserId == request.UserId && entry.Scheduled >= start && entry.Scheduled < end.AddDays(1));

        if (request.Status.HasValue)
        {
            query = query.Where(entry => entry.Status == request.Status.Value);
        }

        var entries = await query
            .OrderBy(entry => entry.Scheduled)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return Array.Empty<ScheduledEntryDto>();
        }
        var workoutids = entries.Select(e => e.WorkoutId).Distinct().ToList();

        var workouts = await (
            from we in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutids.Contains(we.WorkoutId)
            join ex in _dbContext.Exercises.AsNoTracking() on we.ExerciseId equals ex.Id
            join m in _dbContext.Muscles.AsNoTracking() on ex.PrimaryMuscleId equals m.Id into muscleGroup
            from m in muscleGroup.DefaultIfEmpty()
            select new
            {
                we.WorkoutId,
                WorkoutExerciseId = we.Id,
                ExerciseId = ex.Id,
                ExerciseName = ex.Name,
                MuscleName = m != null ? m.Name : "Other",
                we.OrderIndex

            })
            .ToListAsync(cancellationToken);

        var workoutExerciseIds = workouts.Select(wd => wd.WorkoutExerciseId).ToList();
        var sets = await _dbContext.Sets.AsNoTracking()
            .Where(s => workoutExerciseIds.Contains(s.WorkoutExerciseId))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, WorkoutStatRow> workoutStat = workoutids.ToDictionary(
            id => id,
            id =>
            {
                var weList = workouts.Where(wd => wd.WorkoutId == id).ToList();
                var weIds = weList.Select(we => we.WorkoutExerciseId).ToList();
                var workoutSets = sets.Where(s => weIds.Contains(s.WorkoutExerciseId)).ToList();
                var volume = workoutSets.Sum(s => (s.Weight ?? 0f) * (s.Reps ?? 0));
                var totalSets = workoutSets.Count;
                var primaryMuscles = weList.Select(we => we.MuscleName).Where(name => name != "Other").Distinct().Take(3).ToArray();
                var exercisePreview = weList.OrderBy(we => we.OrderIndex).Select(we => we.ExerciseName).Distinct().Take(3).ToArray();
                var exercisePreviewIds = weList.OrderBy(we => we.OrderIndex).Select(we => we.ExerciseId).Distinct().Take(8).ToArray();
                var exerciseCount = weList.Select(we => we.ExerciseId).Distinct().Count();

                return new WorkoutStatRow(volume, totalSets, primaryMuscles, exercisePreview, exercisePreviewIds, exerciseCount);
            }
        );
        var workoutNames = await _dbContext.Workouts.AsNoTracking()
            .Where(w => workoutids.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        //The rest of the code gets stats for completed workouts 
        var entryids = entries
            .Where(e => e.Status == ScheduleStatus.Completed)
            .Select(e => e.Id)
            .ToList();

        var logs = new Dictionary<Guid, WorkoutLog>();
        var completedStats = new Dictionary<Guid, (float Volume, int TotalSets)>();
        var completedPrCounts = new Dictionary<Guid, int>();

        if (entryids.Count > 0)
        {
            logs = await _dbContext.WorkoutLogs.AsNoTracking()
                .Where(l => l.EntryId.HasValue && entryids.Contains(l.EntryId.Value))
                .ToDictionaryAsync(l => l.EntryId!.Value, l => l, cancellationToken);

            var logIds = logs.Values.Select(l => l.Id).ToList();
            if (logIds.Count > 0)
            {
                completedStats = await _dbContext.WorkoutLogSets.AsNoTracking()
                    .Where(s => logIds.Contains(s.LogId))
                    .GroupBy(s => s.LogId)
                    .Select(g => new
                    {
                        LogId = g.Key,
                        Volume = g.Sum(s => s.Weight * s.Reps),
                        TotalSets = g.Count()
                    })
                    .ToDictionaryAsync(x => x.LogId, x => (x.Volume, x.TotalSets), cancellationToken);

                completedPrCounts = await (
                    from exercisePr in _dbContext.ExercisePrs.AsNoTracking()
                    join workoutLogSet in _dbContext.WorkoutLogSets.AsNoTracking() on exercisePr.WorkoutLogSetId equals workoutLogSet.Id
                    where logIds.Contains(workoutLogSet.LogId)
                    group exercisePr by workoutLogSet.LogId into grouped
                    select new
                    {
                        LogId = grouped.Key,
                        PrCount = grouped.Count()
                    })
                    .ToDictionaryAsync(item => item.LogId, item => item.PrCount, cancellationToken);

            }
        }

        return entries
            .Where(entry => entry.Status != ScheduleStatus.Completed || logs.ContainsKey(entry.Id))
            .Select(entry => CreateScheduledEntryDto(entry, workoutStat, workoutNames, logs, completedStats, completedPrCounts))
            .ToList();
    }

    private static ScheduledEntryDto CreateScheduledEntryDto(
        ScheduledEntry entry,
        IReadOnlyDictionary<Guid, WorkoutStatRow> workoutStat,
        IReadOnlyDictionary<Guid, string> workoutNames,
        IReadOnlyDictionary<Guid, WorkoutLog> logs,
        IReadOnlyDictionary<Guid, (float Volume, int TotalSets)> completedStats,
        IReadOnlyDictionary<Guid, int> completedPrCounts)
    {
        workoutStat.TryGetValue(entry.WorkoutId, out var stats);
        workoutNames.TryGetValue(entry.WorkoutId, out var name);
        logs.TryGetValue(entry.Id, out var log);

        float volume = 0f;
        int totalSets = 0;
        if (entry.Status == ScheduleStatus.Completed && log != null && completedStats.TryGetValue(log.Id, out var completedStat))
        {
            volume = completedStat.Volume;
            totalSets = completedStat.TotalSets;
        }
        else if (stats is not null)
        {
            volume = stats.Volume;
            totalSets = stats.TotalSets;
        }

        var prCount = entry.Status == ScheduleStatus.Completed && log != null && completedPrCounts.TryGetValue(log.Id, out var count)
            ? count
            : 0;

        return new ScheduledEntryDto(
            entry.Id,
            entry.WorkoutId,
            name ?? "Unknown Workout",
            entry.Scheduled,
            entry.Status.ToString(),
            stats?.PrimaryMuscleGroups ?? Array.Empty<string>(),
            stats?.ExerciseCount ?? 0,
            stats?.ExercisePreview ?? Array.Empty<string>(),
            stats?.ExercisePreviewIds ?? Array.Empty<Guid>(),
            volume,
            totalSets,
            log?.StartedAt,
            log?.CompletedAt,
            prCount,
            log?.Id
        );
    }

    private sealed record WorkoutStatRow(
        float Volume,
        int TotalSets,
        string[] PrimaryMuscleGroups,
        string[] ExercisePreview,
        Guid[] ExercisePreviewIds,
        int ExerciseCount);
}