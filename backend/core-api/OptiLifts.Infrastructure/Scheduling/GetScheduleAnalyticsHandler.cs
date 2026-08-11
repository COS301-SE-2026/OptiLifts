using MediatR;
using Microsoft.EntityFrameworkCore;
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
        string MuscleName,
        string[] SecondaryMuscles);
    public GetScheduleAnalyticsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ScheduleAnalyticsDto> Handle(GetScheduleAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var (start, end) = ResolveDates(request.StartDate, request.EndDate);

        //add status filtering
        var query = _dbContext.ScheduledEntries.AsNoTracking()
            .Where(entry => entry.UserId == request.UserId && entry.Scheduled >= start && entry.Scheduled < end.AddDays(1));

        if (request.Status.HasValue)
        {
            query = query.Where(entry => entry.Status == request.Status.Value);
        }

        var entries = await query
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return new ScheduleAnalyticsDto(
                TotalWorkouts: 0,
                TotalVolume: 0,
                TotalSets: 0,
                MuscleDistribution: Array.Empty<MuscleDistributionDto>(),
                SecondaryMuscleDistribution: Array.Empty<MuscleDistributionDto>()
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
                m != null ? m.Name : "Other",
                Array.Empty<string>()

            ))
            .ToListAsync(cancellationToken);

        var secondaryMuscleRows = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutids.Contains(workoutExercise.WorkoutId)
            join secondary in _dbContext.SecMuscles.AsNoTracking() on workoutExercise.ExerciseId equals secondary.ExerciseId
            join muscle in _dbContext.Muscles.AsNoTracking() on secondary.MuscleId equals muscle.Id
            select new
            {
                workoutExercise.Id,
                muscle.Name
            })
            .ToListAsync(cancellationToken);

        var secondaryMusclesByExerciseId = secondaryMuscleRows
            .GroupBy(entry => entry.Id)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Name).Distinct().ToArray());

        workoutDetails = workoutDetails
            .Select(detail => detail with
            {
                SecondaryMuscles = secondaryMusclesByExerciseId.TryGetValue(detail.WorkoutExerciseId, out var secondaryMuscles)
                    ? secondaryMuscles
                    : []
            })
            .ToList();

        var workoutExerciseIds = workoutDetails.Select(wd => wd.WorkoutExerciseId).ToList();

        var sets = await _dbContext.Sets.AsNoTracking()
            .Where(s => workoutExerciseIds.Contains(s.WorkoutExerciseId))
            .ToListAsync(cancellationToken);

        //helper function
        var statsEachWorkout = CalculateStatsPerWorkout(workoutids, workoutDetails, sets);

        //helper function
        var (totalVolume, totalSets) = CalculateTotals(entries, statsEachWorkout);

        //helper function 
        var (muscleDistr, secondaryMuscleDistr) = CalculateMuscleDistribution(entries, workoutDetails, sets);

        return new ScheduleAnalyticsDto(
            entries.Count,
            totalVolume,
            totalSets,
            muscleDistr,
            secondaryMuscleDistr
        );
    }
    //helpers to reduce complexity - sonarqube issue
    private static (DateTime Start, DateTime End) ResolveDates(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return (DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc), DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc));
        }

        var now = DateTime.UtcNow.Date;
        int dif = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        var start = DateTime.SpecifyKind(now.AddDays(-dif), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(start.AddDays(6), DateTimeKind.Utc);
        return (start, end);

    }

    //calculate muscle distribution
    private static (List<MuscleDistributionDto> Primary, List<MuscleDistributionDto> Secondary) CalculateMuscleDistribution(
        List<ScheduledEntry> entries,
        List<WorkoutDetailDto> workoutDetails,
        List<WorkoutSet> sets)
    {
        var entryCountsByWorkoutId = entries
            .GroupBy(entry => entry.WorkoutId)
            .ToDictionary(group => group.Key, group => group.Count());

        var primaryCounts = new Dictionary<string, int>();
        var secondaryCounts = new Dictionary<string, int>();

        foreach (var entryCountPair in entryCountsByWorkoutId)
        {
            var workoutId = entryCountPair.Key;
            var repetitions = entryCountPair.Value;
            var workoutExercises = workoutDetails.Where(detail => detail.WorkoutId == workoutId).ToList();
            var workoutExerciseIds = workoutExercises.Select(detail => detail.WorkoutExerciseId).ToHashSet();
            var setsByExerciseId = sets
                .Where(set => workoutExerciseIds.Contains(set.WorkoutExerciseId))
                .GroupBy(set => set.WorkoutExerciseId)
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (var exercise in workoutExercises)
            {
                if (!setsByExerciseId.TryGetValue(exercise.WorkoutExerciseId, out var setCount) || setCount <= 0)
                {
                    continue;
                }

                var totalOccurrences = setCount * repetitions;

                if (exercise.MuscleName != "Other")
                {
                    primaryCounts[exercise.MuscleName] = primaryCounts.TryGetValue(exercise.MuscleName, out var currentPrimary)
                        ? currentPrimary + totalOccurrences
                        : totalOccurrences;
                }

                foreach (var secondaryMuscle in exercise.SecondaryMuscles.Distinct())
                {
                    secondaryCounts[secondaryMuscle] = secondaryCounts.TryGetValue(secondaryMuscle, out var currentSecondary)
                        ? currentSecondary + totalOccurrences
                        : totalOccurrences;
                }
            }
        }

        return (ToDistribution(primaryCounts), ToDistribution(secondaryCounts));
    }

    private static List<MuscleDistributionDto> ToDistribution(Dictionary<string, int> counts)
    {
        var total = counts.Values.Sum();

        return counts
            .OrderByDescending(pair => pair.Value)
            .Select(pair => new MuscleDistributionDto(
                pair.Key,
                pair.Value,
                total > 0 ? (float)pair.Value / total * 100f : 0f
            ))
            .ToList();
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
        foreach (var entry in entries)
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