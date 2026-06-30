using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql.Replication.PgOutput.Messages;
using OptiLifts.Application.Scheduling.GetScheduleAnalytics;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class GetScheduleAnalyticsHandler : IRequestHandler<GetScheduleAnalyticsQuery, ScheduleAnalyticsDto>
{
    private readonly OptiLiftsDbContext _dbContext;

    private sealed record WorkoutDetailDto(
        Guid WorkoutId,
        Guid WorkoutExerciseId,
        string MuscleName);
    public GetScheduleAnalyticsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ScheduleAnalyticsDto> Handle(GetScheduleAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = ResolveDates(request.StartDate, request.EndDate);

        var endLim = end.AddDays(1);
        var entries = await _dbContext.ScheduledEntries.AsNoTracking()
            .Where(entry => entry.UserId == request.UserId && entry.Scheduled >= start && entry.Scheduled < endLim)
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
            select new WorkoutDetailDto
            (
                we.WorkoutId,
                we.Id,
                m != null ? m.Name : "Other"

            ))
            .ToListAsync(cancellationToken);
        
        var workoutExerciseIds = workoutDetails.Select(wd => wd.WorkoutExerciseId).ToList();

        var sets = await _dbContext.Sets.AsNoTracking()
            .Where(s => workoutExerciseIds.Contains(s.WorkoutExerciseId))
            .ToListAsync(cancellationToken);

        //helper function
        var statsEachWorkout = CalculateStatsPerWorkout(workoutids, workoutDetails, sets);

        //helper function
        var (totalVolume, totalSets) = CalculateTotals(entries, statsEachWorkout);

        //helper function 
        var muscleDistr = CalculateMuscleDistribution(entries, workoutDetails, sets);
        
        return new ScheduleAnalyticsDto(
            entries.Count,
            totalVolume,
            totalSets,
            muscleDistr
        );
    }
    //helpers to reduce complexity - sonarqube issue
    private static (DateTime Start, DateTime End) ResolveDates(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return (startDate.Value.Date, endDate.Value.Date);
        }
        
        var now = DateTime.UtcNow.Date;
        int dif = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        var start = now.AddDays(-dif);
        var end = start.AddDays(6);
        return (start, end);
        
    }

    //calculate muscle distribution
    private static List<MuscleDistributionDto> CalculateMuscleDistribution(
        List<ScheduledEntry> entries,
        List<WorkoutDetailDto> workoutDetails,
        List<WorkoutSet> sets)
    {
        var muscleGroups = from entry in entries
            join detail in workoutDetails on entry.WorkoutId equals detail.WorkoutId
            join s in sets on detail.WorkoutExerciseId equals s.WorkoutExerciseId
            where detail.MuscleName != "Other"
            group s by detail.MuscleName into g
            select new MuscleDistributionDto(
                g.Key,
                g.Count(),
                0f
            );
        var result = muscleGroups.ToList();
        var totalSets = result.Sum(r => r.SetCount);
        return result.Select(r => new MuscleDistributionDto(
            r.MuscleGroup,
            r.SetCount,
            totalSets > 0 ? (float)r.SetCount / totalSets * 100f : 0f
        )).ToList();
    }

    //calculate the volume and set counts per workout
    private static Dictionary<Guid, (float Volume, int SetsCount)> CalculateStatsPerWorkout(
        List<Guid> workoutIds,
        List<WorkoutDetailDto> workoutDetails,
        List<WorkoutSet> sets)
    {
        return workoutIds.ToDictionary( 
            id => id,
            id =>
            {
                var weList = workoutDetails.Where(wd => wd.WorkoutId == id).ToList();
                var weIds = weList.Select(we => we.WorkoutExerciseId).ToList();
                var workoutSets = sets.Where(s => weIds.Contains(s.WorkoutExerciseId)).ToList();

                return 
                (
                    Volume: workoutSets.Sum(s => (s.Weight ?? 0f) * (s.Reps ?? 0)),
                    SetsCount: workoutSets.Count
                );
            }
        );

    }

    //calculate total volume and set count
    private static (float TotalVolume, int TotalSets) CalculateTotals(
        List<ScheduledEntry> entries,
        Dictionary<Guid, (float Volume, int SetCount)> statsPerWorkout)
    {
        float totalVolume = 0;
        int totalSets = 0;
        foreach(var entry in entries)
        {
            if (statsPerWorkout.TryGetValue(entry.WorkoutId, out var stat))
            {
                totalVolume += stat.Volume;
                totalSets += stat.SetCount;
            }
        }
        return (totalVolume, totalSets);
    }
    
    

}