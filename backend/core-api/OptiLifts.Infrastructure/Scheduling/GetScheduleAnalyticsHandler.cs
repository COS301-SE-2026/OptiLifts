using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetScheduleAnalytics;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class GetScheduleAnalyticsHandler : IRequestHandler<GetScheduleAnalyticsQuery, ScheduleAnalyticsDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    public GetScheduleAnalyticsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ScheduleAnalyticsDto> Handle(GetScheduleAnalyticsQuery request, CancellationToken cancellationToken)
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
        var entries = await _dbContext.ScheduledEntries.AsNoTracking()
            .Where(entry => entry.UserId == request.UserId && entry.Scheduled >= start && entry.Scheduled < end.AddDays(1))
            .OrderBy(entry => entry.Scheduled)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return new ScheduleAnalyticsDto(
                TotalWorkouts: 0,
                TotalVolume: 0,
                TotalSets: 0,
                MuscleDistribution: Array.Empty<MuscleDistributionDto>()
            );
        }

        var workoutids = entries.Select(e => e.WorkoutId).Distinct().ToList();
        var workoutDetails = await (
            from we in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutids.Contains(we.WorkoutId)
            join ex in _dbContext.Exercises.AsNoTracking() on we.ExerciseId equals ex.Id
            join m in _dbContext.Muscles.AsNoTracking() on ex.PrimaryMuscleId equals m.Id into muscleGroup
            from m in muscleGroup.DefaultIfEmpty()
            select new
            {
                we.WorkoutId,
                WorkoutExerciseId = we.Id,
                MuscleName = m != null ? m.Name : "Other"

            })
            .ToListAsync(cancellationToken);
        
        var workoutExerciseIds = workoutDetails.Select(wd => wd.WorkoutExerciseId).ToList();

        var sets = await _dbContext.Sets.AsNoTracking()
            .Where(s => workoutExerciseIds.Contains(s.WorkoutExerciseId))
            .ToListAsync(cancellationToken);
        var statsEachWorkout = workoutids.ToDictionary(
            id => id,
            id =>
            {
                var weList = workoutDetails.Where(wd => wd.WorkoutId == id).ToList();
                var weIds = weList.Select(we => we.WorkoutExerciseId).ToList();
                var workoutSets = sets.Where(s => weIds.Contains(s.WorkoutExerciseId)).ToList();

                return new
                {
                    Volume = workoutSets.Sum(s => (s.Weight ?? 0f) * (s.Reps ?? 0)),
                    SetsCount = workoutSets.Count
                };
            }
        );

        var totalWorkouts = entries.Count;
        float TotalVolume = 0;
        int totalSets = 0;

        foreach(var entry in entries)
        {
            if (statsEachWorkout.TryGetValue(entry.WorkoutId, out var stat))
            {
                TotalVolume += stat.Volume;
                totalSets += stat.SetsCount;
            }
        }
        var muscleSet = new Dictionary<string, int>();
        foreach(var entry in entries)
        {
            var weList = workoutDetails.Where(wd => wd.WorkoutId == entry.WorkoutId).ToList();
            foreach(var we in weList)
            {
                var setCount = sets.Count(s => s.WorkoutExerciseId == we.WorkoutExerciseId);
                if (setCount > 0 && we.MuscleName != "Other")
                {
                    if (muscleSet.ContainsKey(we.MuscleName))
                    {
                        muscleSet[we.MuscleName] += setCount;
                    }
                    else
                    {
                        muscleSet[we.MuscleName] = setCount;
                    }
                }
            }
        }

        var totalUsedSets = muscleSet.Values.Sum();
        var muscleDistr = muscleSet.Select(keyvalue => new MuscleDistributionDto(
            keyvalue.Key,
            keyvalue.Value,
            totalUsedSets > 0? (float)keyvalue.Value / totalUsedSets * 100f : 0f
        )).OrderByDescending(md => md.SetCount).ToList();

        return new ScheduleAnalyticsDto(
            totalWorkouts,
            TotalVolume,
            totalSets,
            muscleDistr
        );
    }
}