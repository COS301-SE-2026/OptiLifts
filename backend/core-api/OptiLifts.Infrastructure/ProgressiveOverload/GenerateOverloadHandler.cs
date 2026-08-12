using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.ProgressiveOverload;
using OptiLifts.Domain.ProgressiveOverload;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.ProgressiveOverload;

public class GenerateOverloadHandler : IRequestHandler<GenerateOverloadCommand, List<PODataPoint>>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GenerateOverloadHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PODataPoint>> Handle(GenerateOverloadCommand request, CancellationToken cancellationToken)
    {
        //part 1: get enough data points and normalize them to e1RM or just reps if it's bodyweight
        var dataPoints = await GetNormalizedDataPointsAsync(request.UserId, request.ExerciseId, cancellationToken);
        if (dataPoints.Count < 4)
        {
            return dataPoints;
        }

        return dataPoints;
    }

    private async Task<List<PODataPoint>> GetNormalizedDataPointsAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);

        if (exercise == null || (exercise.ExerciseType != ExerciseType.WeightReps && exercise.ExerciseType != ExerciseType.BodyweightReps))
        {
            return new List<PODataPoint>();
        }

        var validWorkouts = await _dbContext.WorkoutLogs
            .AsNoTracking()
            .Join(_dbContext.ScheduledEntries,
                log => log.EntryId,
                entry => entry.Id,
                (log, entry) => new { log, entry })
            .Where(x => x.entry.UserId == userId && x.log.CompletedAt != null)
            .OrderByDescending(x => x.log.StartedAt)
            .Select(x => new
            {
                WorkoutDate = x.log.StartedAt,
                Sets = _dbContext.WorkoutLogSets
                    .Where(s => s.LogId == x.log.Id && s.ExerciseId == exerciseId && s.Type == SetType.Normal)
                    .ToList()
            })
            .Where(x => x.Sets.Any())
            .Take(8)
            .ToListAsync(cancellationToken);


        if (validWorkouts.Count < 4)
        {
            return new List<PODataPoint>();
        }

        int count = 1;
        for (int i = 0; i < validWorkouts.Count - 1; i++)
        {
            if ((validWorkouts[i].WorkoutDate - validWorkouts[i + 1].WorkoutDate).TotalDays > 14)
            {
                break;
            }

            ++count;
        }

        var acceptedWorkouts = validWorkouts.Take(count).ToList();
        if (acceptedWorkouts.Count < 4)
        {
            return new List<PODataPoint>();
        }

        var dataPoints = new List<PODataPoint>();
        foreach (var w in acceptedWorkouts)
        {
            float avgWeight = w.Sets.Average(s => s.Weight);
            int avgReps = (int)Math.Round(w.Sets.Average(s => s.Reps));

            var e1RM = E1RMCalculator.CalculateE1RM(avgWeight, avgReps, exercise.Mechanic, exercise.ExerciseType);
            dataPoints.Add(new PODataPoint(w.WorkoutDate, e1RM));
        }

        return dataPoints;
    }


}