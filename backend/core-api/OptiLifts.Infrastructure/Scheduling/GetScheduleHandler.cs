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
            start = request.StartDate.Value.Date;
            end = request.EndDate.Value.Date;
        }
        else
        {
            var now = DateTime.UtcNow.Date;
            int dif = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            start = now.AddDays(-dif);
            end = start.AddDays(6);
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

        var workoutStat = workoutids.ToDictionary(
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
                var exerciseCount = weList.Select(we => we.ExerciseId).Distinct().Count();

                return new
                {
                    Volume = volume,
                    TotalSets = totalSets,
                    PrimaryMuscleGroups = primaryMuscles,
                    ExercisePreview = exercisePreview,
                    ExerciseCount = exerciseCount
                };
            }
        );
        var workoutNames = await _dbContext.Workouts.AsNoTracking()
            .Where(w => workoutids.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        var entryids = entries
            .Where(e => e.Status == ScheduleStatus.Completed)
            .Select(e => e.Id)
            .ToList();

        var logs = new Dictionary<Guid, OptiLifts.Domain.Workouts.WorkoutLog>();
        if (entryids.Count > 0)
        {
            logs = await _dbContext.WorkoutLogs.AsNoTracking()
                .Where(l => l.EntryId.HasValue && entryids.Contains(l.EntryId.Value))
                .ToDictionaryAsync(l => l.EntryId!.Value, l => l, cancellationToken);
        }

        //will change once PR table is implemented
        var PRs = 1;

        var scheduledEntryDtos = entries.Select(entry =>
        {
            workoutStat.TryGetValue(entry.WorkoutId, out var stats);
            workoutNames.TryGetValue(entry.WorkoutId, out var name);

            logs.TryGetValue(entry.Id, out var log);

            return new ScheduledEntryDto(
                entry.Id,
                entry.WorkoutId,
                name ?? "Unknown Workout",
                entry.Scheduled,
                entry.Status.ToString(),
                stats?.PrimaryMuscleGroups ?? Array.Empty<string>(),
                stats?.ExerciseCount ?? 0,
                stats?.ExercisePreview ?? Array.Empty<string>(),
                stats?.Volume ?? 0f,
                stats?.TotalSets ?? 0,
                log?.StartedAt,
                log?.CompletedAt,
                PRs,
                log?.Id
            );
        }).ToList();
        return scheduledEntryDtos;
    }
}