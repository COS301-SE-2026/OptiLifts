using System.Globalization;
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
        var dataPoints = await GetNormalizedDataPointsAsync(request.UserId, request.ExerciseId, cancellationToken);
        if (dataPoints.Count < 4)
        {
            await RemoveEstimationAsync(request.UserId, request.ExerciseId, cancellationToken);
            return dataPoints;
        }
        double predictedMetric = BestFitEngine.PredictNextVal(dataPoints);
        if (predictedMetric <= 0)
        {
            await RemoveEstimationAsync(request.UserId, request.ExerciseId, cancellationToken);
            return dataPoints;
        }

        var recommendation = await BuildRecommendationAsync(
            request.UserId,
            request.ExerciseId,
            predictedMetric,
            cancellationToken);

        if (recommendation is null)
        {
            await RemoveEstimationAsync(request.UserId, request.ExerciseId, cancellationToken);
            return dataPoints;
        }

        await UpsertEstimationAsync(request.UserId, request.ExerciseId, recommendation, cancellationToken);
        var projectedDate = GetProjectedNextDate(dataPoints);
        dataPoints.Insert(0, new PODataPoint(projectedDate, recommendation.Metric));

        return dataPoints;
    }

    private async Task<OverloadRecommendation?> BuildRecommendationAsync(Guid userId, Guid exerciseId, double predictedMetric, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);

        if (exercise == null || !IsSupportedExerciseType(exercise.ExerciseType))
        {
            return null;
        }

        if (exercise.ExerciseType == ExerciseType.BodyweightReps)
        {
            return await BuildBodyweightRecommendationAsync(userId, exerciseId, exercise, cancellationToken);
        }

        var (lowerLimit, upperLimit) = await GetUserRepRangeAsync(userId, exercise.Mechanic, cancellationToken);

        float bodyweightForWeighted = 0f;
        if (exercise.ExerciseType == ExerciseType.WeightedBodyweight)
        {
            var userBodyweight = await GetUserBodyweightAsync(userId, cancellationToken);
            if (!userBodyweight.HasValue || userBodyweight.Value <= 0)
            {
                return null;
            }

            bodyweightForWeighted = userBodyweight.Value;
        }

        var previousWeight = await GetPreviousAverageWeightAsync(userId, exerciseId, cancellationToken);
        if (!previousWeight.HasValue || previousWeight.Value < 0 ||
            (exercise.ExerciseType == ExerciseType.WeightReps && previousWeight.Value == 0))
        {
            return null;
        }

        int weightedCalculatedReps = E1RMCalculator.ReverseEpleyReps(predictedMetric, previousWeight.Value, exercise.Mechanic, exercise.ExerciseType, bodyweightForWeighted);

        //Check if newly calculated reps is in range 
        //if yes, reps are in range, keep same weight and use new reps.
        if (IsRepsInRange(weightedCalculatedReps, lowerLimit, upperLimit))
        {
            return CreateWeightedRecommendation(previousWeight.Value, weightedCalculatedReps, exercise, bodyweightForWeighted);
        }

        //if no, attempt weight increase at lower rep limit.
        float predictedTargetWeight = E1RMCalculator.ReverseEpleyWeight(predictedMetric, lowerLimit, exercise.Mechanic, exercise.ExerciseType, bodyweightForWeighted);

        if (IsMachineExercise(exercise))
        {
            float truncatedTargetWeight = (float)Math.Truncate(predictedTargetWeight);
            if (truncatedTargetWeight <= previousWeight.Value)
            {
                int forcedRepIncrease = GetForcedRepIncrease(weightedCalculatedReps, upperLimit);
                return CreateWeightedRecommendation(previousWeight.Value, forcedRepIncrease, exercise, bodyweightForWeighted);
            }

            return CreateWeightedRecommendation(truncatedTargetWeight, lowerLimit, exercise, bodyweightForWeighted);
        }

        //For now, all non machine weighted exercises use 5kg increments.
        //Want to discuss so remind me when the time comes.
        float roundedTargetWeight = (float)Math.Floor(predictedTargetWeight / WeightedIncrementKg) * WeightedIncrementKg;

        //If no valid increment step increase is possible, increase reps regardless of upper rep range limit.
        if (roundedTargetWeight <= previousWeight.Value)
        {
            int forcedRepIncrease = GetForcedRepIncrease(weightedCalculatedReps, upperLimit);
            return CreateWeightedRecommendation(previousWeight.Value, forcedRepIncrease, exercise, bodyweightForWeighted);
        }

        return CreateWeightedRecommendation(roundedTargetWeight, lowerLimit, exercise, bodyweightForWeighted);
    }

    private async Task<OverloadRecommendation?> BuildBodyweightRecommendationAsync(Guid userId, Guid exerciseId, Exercise exercise, CancellationToken cancellationToken)
    {
        var bodyweight = await GetUserBodyweightAsync(userId, cancellationToken);
        if (!bodyweight.HasValue || bodyweight.Value <= 0)
        {
            return null;
        }

        var previousReps = await GetPreviousAverageRepsAsync(userId, exerciseId, cancellationToken);
        if (!previousReps.HasValue)
        {
            return null;
        }

        int recommendedReps = previousReps.Value + 1;
        double metric = E1RMCalculator.CalculateE1RM(0f, recommendedReps, exercise.Mechanic, exercise.ExerciseType, bodyweight.Value);
        return new OverloadRecommendation(null, recommendedReps, metric, exercise.ExerciseType);
    }

    private static bool IsSupportedExerciseType(ExerciseType exerciseType)
    {
        return exerciseType == ExerciseType.WeightReps
               || exerciseType == ExerciseType.BodyweightReps
               || exerciseType == ExerciseType.WeightedBodyweight;
    }

    private async Task<float?> GetUserBodyweightAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || !float.TryParse(user.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var bodyweight))
        {
            return null;
        }

        return bodyweight;
    }

    private static int GetForcedRepIncrease(int calculatedReps, int upperLimit)
    {
        if (calculatedReps <= upperLimit || calculatedReps == int.MaxValue)
        {
            return upperLimit + 1;
        }

        return calculatedReps;
    }

    private static OverloadRecommendation CreateWeightedRecommendation(float weight, int reps, Exercise exercise, float bodyweight = 0f)
    {
        return new OverloadRecommendation(
            weight,
            reps,
            E1RMCalculator.CalculateE1RM(weight, reps, exercise.Mechanic, exercise.ExerciseType, bodyweight),
            exercise.ExerciseType);
    }

    private async Task UpsertEstimationAsync(Guid userId, Guid exerciseId, OverloadRecommendation recommendation, CancellationToken cancellationToken)
    {
        var estimations = await _dbContext.ExerciseEstimations
            .Where(estimation => estimation.UserId == userId && estimation.ExerciseId == exerciseId)
            .OrderByDescending(estimation => estimation.TimeStamp)
            .ToListAsync(cancellationToken);

        var estimation = estimations.FirstOrDefault();
        if (estimation is null)
        {
            estimation = new ExerciseEstimation
            {
                UserId = userId,
                ExerciseId = exerciseId
            };
            _dbContext.ExerciseEstimations.Add(estimation);
        }

        if (estimations.Count > 1)
        {
            _dbContext.ExerciseEstimations.RemoveRange(estimations.Skip(1));
        }

        estimation.Weight = recommendation.Weight;
        estimation.Reps = recommendation.Reps;
        estimation.ExerciseType = recommendation.ExerciseType;
        estimation.TimeStamp = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveEstimationAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken)
    {
        var estimations = await _dbContext.ExerciseEstimations
            .Where(estimation => estimation.UserId == userId && estimation.ExerciseId == exerciseId)
            .ToListAsync(cancellationToken);

        if (estimations.Count == 0)
        {
            return;
        }

        _dbContext.ExerciseEstimations.RemoveRange(estimations);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsMachineExercise(Exercise exercise)
    {
        return !string.IsNullOrWhiteSpace(exercise.Equipment) && exercise.Equipment.Contains("machine", StringComparison.OrdinalIgnoreCase);
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

    private async Task<int?> GetPreviousAverageRepsAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken)
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

        return (int)Math.Round(latestWorkout.Sets.Average(s => s.Reps));
    }

    private async Task<List<PODataPoint>> GetNormalizedDataPointsAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);

        if (exercise == null || !IsSupportedExerciseType(exercise.ExerciseType))
        {
            return new List<PODataPoint>();
        }

        float bodyweight = 0f;
        if (exercise.ExerciseType == ExerciseType.BodyweightReps || exercise.ExerciseType == ExerciseType.WeightedBodyweight)
        {
            var userBodyweight = await GetUserBodyweightAsync(userId, cancellationToken);
            if (!userBodyweight.HasValue || userBodyweight.Value <= 0)
            {
                return new List<PODataPoint>();
            }

            bodyweight = userBodyweight.Value;
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

            var e1RM = E1RMCalculator.CalculateE1RM(avgWeight, avgReps, exercise.Mechanic, exercise.ExerciseType, bodyweight);
            dataPoints.Add(new PODataPoint(w.WorkoutDate, e1RM));
        }

        return dataPoints;
    }

    private sealed record OverloadRecommendation(float? Weight, int Reps, double Metric, ExerciseType ExerciseType);
}