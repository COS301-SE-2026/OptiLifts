using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.ProgressiveOverload;
using OptiLifts.Domain.ProgressiveOverload;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.ProgressiveOverload;

public class GenerateOverloadHandler : IRequestHandler<GenerateOverloadCommand, List<PODataPoint>>
{
    private const float WeightedIncrementKg = 5f;
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

        //part 2: gradient calculation and e1RM prediction
        if (BestFitEngine.PlateauCheck(dataPoints))
        {
            //platea does its cool quirky thing
        }

        //if bodyweight will be x reps, if weight will be e1RM in weight
        double predictedMetric = BestFitEngine.PredictNextVal(dataPoints);

        var recommendationMetric = await BuildRecommendationMetricAsync(
            request.UserId,
            request.ExerciseId,
            predictedMetric,
            cancellationToken);

        var projectedDate = GetProjectedNextDate(dataPoints);
        dataPoints.Insert(0, new PODataPoint(projectedDate, recommendationMetric));


        return dataPoints;
    }

    private async Task<double> BuildRecommendationMetricAsync(Guid userId, Guid exerciseId, double predictedMetric, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);

        if (exercise == null || (exercise.ExerciseType != ExerciseType.WeightReps && exercise.ExerciseType != ExerciseType.BodyweightReps))
        {
            return predictedMetric;
        }

        var (lowerLimit, upperLimit) = await GetUserRepRangeAsync(userId, exercise.Mechanic, cancellationToken);

        if (exercise.ExerciseType == ExerciseType.BodyweightReps)
        {
            int calculatedReps = E1RMCalculator.ReverseEpleyReps(predictedMetric, 0f, exercise.Mechanic, exercise.ExerciseType);

            if (IsRepsInRange(calculatedReps, lowerLimit, upperLimit))
            {
                return predictedMetric;
            }

            int clampedReps = ClampRepsToRange(calculatedReps, lowerLimit, upperLimit);
            return clampedReps;
        }

        var previousWeight = await GetPreviousAverageWeightAsync(userId, exerciseId, cancellationToken);
        if (!previousWeight.HasValue)
        {
            return predictedMetric;
        }

        int weightedCalculatedReps = E1RMCalculator.ReverseEpleyReps(predictedMetric, previousWeight.Value, exercise.Mechanic, exercise.ExerciseType);

        //Check if newly calculated reps is in range 
        //if yes, reps are in range, keep same weight and use new reps.
        if (IsRepsInRange(weightedCalculatedReps, lowerLimit, upperLimit))
        {
            return E1RMCalculator.CalculateE1RM(previousWeight.Value, weightedCalculatedReps, exercise.Mechanic, exercise.ExerciseType);
        }

        //if no, attempt weight increase at lower rep limit.
        float predictedTargetWeight = E1RMCalculator.ReverseEpleyWeight(predictedMetric, lowerLimit, exercise.Mechanic, exercise.ExerciseType);

        //Machine exercise, weight recomendation given as is.
        if (IsMachineExercise(exercise))
        {
            if (predictedTargetWeight <= previousWeight.Value)
            {
                int forcedRepIncrease = Math.Max(weightedCalculatedReps, upperLimit + 1);
                return E1RMCalculator.CalculateE1RM(previousWeight.Value, forcedRepIncrease, exercise.Mechanic, exercise.ExerciseType);
            }

            return E1RMCalculator.CalculateE1RM(predictedTargetWeight, lowerLimit, exercise.Mechanic, exercise.ExerciseType);
        }

        //For now, all non machine weighted exercises use 5kg increments.
        //Want to discuss so remind me when the time comes.
        float predictedIncrease = predictedTargetWeight - previousWeight.Value;
        float validIncrease = (float)Math.Floor(predictedIncrease / WeightedIncrementKg) * WeightedIncrementKg;

        //If no valid increment step increase is possible, increase reps regardless of upper rep range limit.
        if (validIncrease < WeightedIncrementKg)
        {
            int forcedRepIncrease = Math.Max(weightedCalculatedReps, upperLimit + 1);
            return E1RMCalculator.CalculateE1RM(previousWeight.Value, forcedRepIncrease, exercise.Mechanic, exercise.ExerciseType);
        }

        float recommendedWeight = previousWeight.Value + validIncrease;
        return E1RMCalculator.CalculateE1RM(recommendedWeight, lowerLimit, exercise.Mechanic, exercise.ExerciseType);
    }

    private static bool IsMachineExercise(Exercise exercise)
    {
        return !string.IsNullOrWhiteSpace(exercise.Equipment)
               && exercise.Equipment.Contains("machine", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime GetProjectedNextDate(List<PODataPoint> points)
    {
        if (points.Count < 2)
        {
            return points[0].Date.AddDays(7);
        }

        double gapTotal = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            gapTotal += (points[i].Date - points[i + 1].Date).TotalDays;
        }

        int avgGap = (int)Math.Round(gapTotal / (points.Count - 1));
        return points[0].Date.AddDays(Math.Max(1, avgGap));
    }

    private static bool IsRepsInRange(int reps, int lowerLimit, int upperLimit)
    {
        return reps >= lowerLimit && reps <= upperLimit;
    }

    private static int ClampRepsToRange(int reps, int lowerLimit, int upperLimit)
    {
        return Math.Clamp(reps, lowerLimit, upperLimit);
    }

    private async Task<(int LowerLimit, int UpperLimit)> GetUserRepRangeAsync(Guid userId, string? mechanic, CancellationToken cancellationToken)
    {
        var exerciseType = String.Equals(mechanic, "compound", StringComparison.OrdinalIgnoreCase)
            ? UserRepRangeExerciseType.Compound
            : UserRepRangeExerciseType.Isolation;

        var repRange = await _dbContext.UserRepRanges
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ExerciseType == exerciseType, cancellationToken);

        if (repRange != null)
        {
            return (repRange.LowerLimit, repRange.UpperLimit);
        }

        if (exerciseType == UserRepRangeExerciseType.Compound)
        {
            return (5, 8);
        }

        return (8, 12);
    }

    private async Task<float?> GetPreviousAverageWeightAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken)
    {
        var latestWorkout = await _dbContext.WorkoutLogs
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
            .FirstOrDefaultAsync(cancellationToken);

        if (latestWorkout == null)
        {
            return null;
        }

        return latestWorkout.Sets.Average(s => s.Weight);
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