using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutDetail;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutDetailHandler : IRequestHandler<GetWorkoutDetailQuery, WorkoutDetailDto?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutDetailHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutDetailDto?> Handle(GetWorkoutDetailQuery request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId && !w.IsDeleted, cancellationToken);

        if (workout is null)
        {
            return null;
        }

        var workoutExercises = await GetInitialWorkoutExercisesAsync(workout.Id, cancellationToken);

        if (request.IsTimeConstrained && workoutExercises.Count > 0)
        {
            workoutExercises = await GenerateTimeConstrainedExercisesAsync(workout, workoutExercises, request.TimeBudgetMinutes, cancellationToken);
        }

        var exerId = workoutExercises.Select(entry => entry.ExerciseId).Distinct().ToArray();
        var bestWeightByExer = await GetBestWeightsAsync(request.UserId, exerId, cancellationToken);
        var bestVolByExer = await GetBestVolumesAsync(request.UserId, exerId, cancellationToken);
        var estimationsByExerciseId = await GetEstimationsAsync(request.UserId, exerId, cancellationToken);
        var secondaryMusclesByExerciseId = await GetSecondaryMusclesAsync(workout.Id, cancellationToken);

        var workoutExerciseIds = workoutExercises.Select(entry => entry.Id).ToArray();
        var previousByWorkoutExerciseId = await GetPreviousPerformanceByWorkoutExerciseIdAsync(request.UserId, workoutExerciseIds, cancellationToken);
        var setsByWorkoutExerciseId = await GetSetsByWorkoutExerciseIdAsync(workoutExerciseIds, previousByWorkoutExerciseId, cancellationToken);

        if (request.IsTimeConstrained)
        {
            await SetupTimeConstrainedSetsAsync(request.UserId, workoutExercises, setsByWorkoutExerciseId, request.TimeBudgetMinutes, cancellationToken);
        }

        var exercises = workoutExercises.Select(entry => BuildExerciseDetailDto(
            entry,
            secondaryMusclesByExerciseId,
            setsByWorkoutExerciseId,
            bestWeightByExer,
            bestVolByExer,
            estimationsByExerciseId)).ToArray();

        var primaryMuscleGroups = exercises
            .Select(exercise => exercise.PrimaryMuscle)
            .Distinct()
            .ToArray();

        var exercisePreview = exercises
            .Select(exercise => exercise.Name)
            .Distinct()
            .Take(3)
            .ToArray();

        return new WorkoutDetailDto(
            workout.Id,
            workout.Name,
            workout.FolderId,
            null,
            workout.CreatedAt,
            primaryMuscleGroups,
            exercisePreview,
            exercises);
    }

    private async Task<Dictionary<Guid, List<(float? Weight, int? Reps)>>> GetPreviousPerformanceByWorkoutExerciseIdAsync(
        Guid userId,
        Guid[] workoutExerciseIds,
        CancellationToken cancellationToken)
    {
        if (workoutExerciseIds.Length == 0)
        {
            return new Dictionary<Guid, List<(float?, int?)>>();
        }

        var loggedSets = await (
            from loggedSet in _dbContext.WorkoutLogSets.AsNoTracking()
            join log in _dbContext.WorkoutLogs.AsNoTracking() on loggedSet.LogId equals log.Id
            join entry in _dbContext.ScheduledEntries.AsNoTracking() on log.EntryId equals entry.Id
            where entry.UserId == userId
                && log.CompletedAt != null
                && loggedSet.WorkoutExerciseId != null
                && workoutExerciseIds.Contains(loggedSet.WorkoutExerciseId.Value)
            select new
            {
                WorkoutExerciseId = loggedSet.WorkoutExerciseId!.Value,
                log.Id,
                log.StartedAt,
                loggedSet.OrderIndex,
                loggedSet.Weight,
                loggedSet.Reps
            })
            .ToListAsync(cancellationToken);

        var latestLogIdByExercise = loggedSets
            .GroupBy(row => row.WorkoutExerciseId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.StartedAt).First().Id);

        return latestLogIdByExercise.ToDictionary(
            pair => pair.Key,
            pair => loggedSets
                .Where(row => row.WorkoutExerciseId == pair.Key && row.Id == pair.Value)
                .OrderBy(row => row.OrderIndex)
                .Select(row => ((float?)row.Weight, (int?)row.Reps))
                .ToList());
    }

    private async Task<Dictionary<Guid, List<WorkoutSetDto>>> GetSetsByWorkoutExerciseIdAsync(
        Guid[] workoutExerciseIds,
        Dictionary<Guid, List<(float? Weight, int? Reps)>> previousByWorkoutExerciseId,
        CancellationToken cancellationToken)
    {
        var setsByWorkoutExerciseId = new Dictionary<Guid, List<WorkoutSetDto>>();

        if (workoutExerciseIds.Length == 0)
        {
            return setsByWorkoutExerciseId;
        }

        var workoutSets = await _dbContext.Sets
            .AsNoTracking()
            .Where(workoutSet => workoutExerciseIds.Contains(workoutSet.WorkoutExerciseId))
            .OrderBy(workoutSet => workoutSet.WorkoutExerciseId)
            .ThenBy(workoutSet => workoutSet.OrderIndex)
            .ToListAsync(cancellationToken);

        foreach (var workoutSet in workoutSets)
        {
            if (!setsByWorkoutExerciseId.TryGetValue(workoutSet.WorkoutExerciseId, out var exerciseSets))
            {
                exerciseSets = [];
                setsByWorkoutExerciseId[workoutSet.WorkoutExerciseId] = exerciseSets;
            }

            var previous = GetPreviousForPosition(previousByWorkoutExerciseId, workoutSet.WorkoutExerciseId, exerciseSets.Count);

            exerciseSets.Add(new WorkoutSetDto(
                workoutSet.Id,
                workoutSet.Type.ToString(),
                workoutSet.Reps,
                workoutSet.Weight,
                workoutSet.Duration,
                workoutSet.Distance,
                workoutSet.OrderIndex,
                workoutSet.RestTime,
                previous.Weight,
                previous.Reps));
        }

        return setsByWorkoutExerciseId;
    }

    private static (float? Weight, int? Reps) GetPreviousForPosition(
        Dictionary<Guid, List<(float? Weight, int? Reps)>> previousByWorkoutExerciseId,
        Guid workoutExerciseId,
        int position)
    {
        if (!previousByWorkoutExerciseId.TryGetValue(workoutExerciseId, out var previousSets) || previousSets.Count == 0)
        {
            return (null, null);
        }

        var clampedIndex = Math.Min(position, previousSets.Count - 1);
        return previousSets[clampedIndex];
    }

    private static WorkoutExerciseDetailDto BuildExerciseDetailDto(
        WorkoutExerciseRow entry,
        Dictionary<Guid, string[]> secondaryMusclesByExerciseId,
        Dictionary<Guid, List<WorkoutSetDto>> setsByWorkoutExerciseId,
        Dictionary<Guid, float> bestWeightByExer,
        Dictionary<Guid, float> bestVolByExer,
        Dictionary<Guid, ExerciseEstimation> estimationsByExerciseId)
    {
        return new WorkoutExerciseDetailDto(
            entry.Id,
            entry.ExerciseId,
            entry.ExerciseName,
            entry.PrimaryMuscleName,
            secondaryMusclesByExerciseId.TryGetValue(entry.ExerciseId, out var secondaryMuscles)
                ? secondaryMuscles
                : [],
            ToFrontendExerciseType(entry.ExerciseType),
            entry.OrderIndex,
            setsByWorkoutExerciseId.TryGetValue(entry.Id, out var workoutSets)
                ? workoutSets.ToArray()
                : [],
            entry.GroupId,
            entry.GroupType,
            entry.GroupRestTime,
            entry.ImageUrl,
            bestWeightByExer.TryGetValue(entry.ExerciseId, out var bestWeight) ? bestWeight : null,
            bestVolByExer.TryGetValue(entry.ExerciseId, out var bestVolume) ? bestVolume : null,
            estimationsByExerciseId.TryGetValue(entry.ExerciseId, out var estimation)
                ? new ExerciseEstimationDto(estimation.Weight, estimation.Reps)
                : null,
            IsMachineExercise(entry.Equipment));
    }

    private static bool IsMachineExercise(string? equipment)
    {
        return !string.IsNullOrWhiteSpace(equipment) && equipment.Contains("machine", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToFrontendExerciseType(OptiLifts.Domain.Workouts.ExerciseType exerciseType)
    {
        return exerciseType switch
        {
            OptiLifts.Domain.Workouts.ExerciseType.WeightReps => "WeightReps",
            OptiLifts.Domain.Workouts.ExerciseType.BodyweightReps => "BodyweightReps",
            OptiLifts.Domain.Workouts.ExerciseType.AssistedWeightReps => "AssistedWeightReps",
            OptiLifts.Domain.Workouts.ExerciseType.WeightedBodyweight => "WeightedBodyWeight",
            OptiLifts.Domain.Workouts.ExerciseType.Duration => "Duration",
            OptiLifts.Domain.Workouts.ExerciseType.DurationWeight => "DurationWeight",
            OptiLifts.Domain.Workouts.ExerciseType.DistanceDuration => "DistanceDuration",
            OptiLifts.Domain.Workouts.ExerciseType.WeightDistance => "WeightDistance",
            _ => exerciseType.ToString()
        };
    }

    private sealed record WorkoutExerciseRow(
        Guid Id,
        Guid ExerciseId,
        int OrderIndex,
        Guid? GroupId,
        string ExerciseName,
        string PrimaryMuscleName,
        OptiLifts.Domain.Workouts.ExerciseType ExerciseType,
        string? GroupType,
        int? GroupRestTime,
        string? ImageUrl,
        string? Equipment);
    private async Task<Dictionary<Guid, List<(float? Weight, int? Reps, int? Duration, float? Distance)>>> GetRecentSetsByExerciseIdAsync(
            Guid userId,
            Guid[] exerciseIds,
            CancellationToken cancellationToken)
    {
        if (exerciseIds.Length == 0) return new Dictionary<Guid, List<(float?, int?, int?, float?)>>();

        var loggedSets = await (
            from loggedSet in _dbContext.WorkoutLogSets.AsNoTracking()
            join log in _dbContext.WorkoutLogs.AsNoTracking() on loggedSet.LogId equals log.Id
            join entry in _dbContext.ScheduledEntries.AsNoTracking() on log.EntryId equals entry.Id
            where entry.UserId == userId
                && log.CompletedAt != null
                && exerciseIds.Contains(loggedSet.ExerciseId)
            select new
            {
                loggedSet.ExerciseId,
                log.Id,
                log.StartedAt,
                loggedSet.OrderIndex,
                loggedSet.Weight,
                loggedSet.Reps,
                loggedSet.Duration,
                loggedSet.Distance
            })
            .ToListAsync(cancellationToken);

        var latestLogIdByExercise = loggedSets
            .GroupBy(row => row.ExerciseId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.StartedAt).First().Id);

        return latestLogIdByExercise.ToDictionary(
            pair => pair.Key,
            pair => loggedSets
                .Where(row => row.ExerciseId == pair.Key && row.Id == pair.Value)
                .OrderBy(row => row.OrderIndex)
                .Select(row => ((float?)row.Weight, (int?)row.Reps, row.Duration, row.Distance))
                .ToList());
    }

    private async Task<List<WorkoutExerciseRow>> GetInitialWorkoutExercisesAsync(
        Guid workoutId,
        CancellationToken cancellationToken)
    {
        return await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == workoutId
            join exercise in _dbContext.Exercises.AsNoTracking()
                on workoutExercise.ExerciseId equals exercise.Id
            join muscle in _dbContext.Muscles.AsNoTracking()
                on exercise.PrimaryMuscleId equals muscle.Id
            join eg in _dbContext.ExerciseGroups.AsNoTracking()
                on workoutExercise.GroupId equals eg.Id into egJoin
            from exerciseGroup in egJoin.DefaultIfEmpty()
            orderby workoutExercise.OrderIndex, exercise.Name
            select new WorkoutExerciseRow(
                workoutExercise.Id,
                workoutExercise.ExerciseId,
                workoutExercise.OrderIndex,
                workoutExercise.GroupId,
                exercise.Name,
                muscle.Name,
                exercise.ExerciseType,
                exerciseGroup != null ? exerciseGroup.Type.ToString() : null,
                (int?)(exerciseGroup != null ? exerciseGroup.RestTime : null),
                exercise.ImageUrl,
                exercise.Equipment))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, float>> GetBestWeightsAsync(
        Guid userId,
        Guid[] exerId,
        CancellationToken cancellationToken)
    {
        var bestVal = await _dbContext.ExercisePrs
            .AsNoTracking()
            .Where(pr => pr.UserId == userId && exerId.Contains(pr.ExerciseId))
            .GroupBy(pr => new { pr.ExerciseId, pr.PrType })
            .Select(group => new { group.Key.ExerciseId, group.Key.PrType, Best = group.Max(pr => pr.PrValue) })
            .ToListAsync(cancellationToken);

        return bestVal
            .Where(item => item.PrType == ExercisePrType.MaxWeight)
            .ToDictionary(item => item.ExerciseId, item => item.Best);
    }

    private async Task<Dictionary<Guid, float>> GetBestVolumesAsync(
        Guid userId,
        Guid[] exerId,
        CancellationToken cancellationToken)
    {
        var bestVal = await _dbContext.ExercisePrs
            .AsNoTracking()
            .Where(pr => pr.UserId == userId && exerId.Contains(pr.ExerciseId))
            .GroupBy(pr => new { pr.ExerciseId, pr.PrType })
            .Select(group => new { group.Key.ExerciseId, group.Key.PrType, Best = group.Max(pr => pr.PrValue) })
            .ToListAsync(cancellationToken);

        return bestVal
            .Where(item => item.PrType == ExercisePrType.MaxSetVolume)
            .ToDictionary(item => item.ExerciseId, item => item.Best);
    }

    private async Task<Dictionary<Guid, ExerciseEstimation>> GetEstimationsAsync(
        Guid userId,
        Guid[] exerId,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.ExerciseEstimations
            .AsNoTracking()
            .Where(estimation => estimation.UserId == userId && exerId.Contains(estimation.ExerciseId))
            .OrderByDescending(estimation => estimation.TimeStamp)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(estimation => estimation.ExerciseId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<Dictionary<Guid, string[]>> GetSecondaryMusclesAsync(
        Guid workoutId,
        CancellationToken cancellationToken)
    {
        var secondaryMuscleRows = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == workoutId
            join secondary in _dbContext.SecMuscles.AsNoTracking()
                on workoutExercise.ExerciseId equals secondary.ExerciseId
            join muscle in _dbContext.Muscles.AsNoTracking()
                on secondary.MuscleId equals muscle.Id
            select new
            {
                workoutExercise.ExerciseId,
                muscle.Name
            })
            .ToListAsync(cancellationToken);

        return secondaryMuscleRows
            .GroupBy(entry => entry.ExerciseId)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Name).Distinct().ToArray());
    }

    private async Task<List<WorkoutExerciseRow>> GenerateTimeConstrainedExercisesAsync(
        Workout workout,
        List<WorkoutExerciseRow> originalExercises,
        int? timeBudgetMinutes,
        CancellationToken cancellationToken)
    {
        var originalPrimaryMuscles = await _dbContext.WorkoutExercises
            .Where(we => we.WorkoutId == workout.Id)
            .Join(_dbContext.Exercises, we => we.ExerciseId, e => e.Id, (we, e) => e.PrimaryMuscleId)
            .ToListAsync(cancellationToken);

        var originalSecondaryMuscles = await _dbContext.WorkoutExercises
            .Where(we => we.WorkoutId == workout.Id)
            .Join(_dbContext.SecMuscles, we => we.ExerciseId, sm => sm.ExerciseId, (we, sm) => sm.MuscleId)
            .ToListAsync(cancellationToken);

        var orderedPrimaryMuscleGroups = originalPrimaryMuscles
            .GroupBy(m => m)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        var topPrimaryMuscleId = orderedPrimaryMuscleGroups.Count > 0 ? (Guid?)orderedPrimaryMuscleGroups[0] : null;
        var muscleFrequency = CalculateMuscleFrequencies(originalPrimaryMuscles, originalSecondaryMuscles);
        var targetMuscleIds = muscleFrequency.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

        var possibleCompounds = await GetPossibleCompoundAsync(originalExercises, cancellationToken);

        int maxExercisesToConsider = timeBudgetMinutes.HasValue
            ? Math.Min(originalExercises.Count, GetBudgetCap(timeBudgetMinutes.Value))
            : originalExercises.Count;

        var selectedCompounds = new List<WorkoutExerciseRow>();
        var coveredPrimaryMuscles = new HashSet<Guid>();
        int orderIndex = 0;

        AddTopMusclePossibility(topPrimaryMuscleId, possibleCompounds, selectedCompounds, coveredPrimaryMuscles, targetMuscleIds, maxExercisesToConsider, ref orderIndex);
        AddDiversePrimaryMusclePossibilities(orderedPrimaryMuscleGroups, possibleCompounds, selectedCompounds, coveredPrimaryMuscles, targetMuscleIds, maxExercisesToConsider, ref orderIndex);
        AddRemainingBudgetPossibilities(possibleCompounds, selectedCompounds, targetMuscleIds, muscleFrequency, maxExercisesToConsider, ref orderIndex);

        return selectedCompounds;
    }

    private static Dictionary<Guid, int> CalculateMuscleFrequencies(
        List<Guid> originalPrimaryMuscles,
        List<Guid> originalSecondaryMuscles)
    {
        var muscleFrequency = new Dictionary<Guid, int>();
        foreach (var m in originalPrimaryMuscles)
        {
            muscleFrequency[m] = muscleFrequency.GetValueOrDefault(m, 0) + 3;
        }
        foreach (var m in originalSecondaryMuscles)
        {
            muscleFrequency[m] = muscleFrequency.GetValueOrDefault(m, 0) + 1;
        }
        return muscleFrequency;
    }

    private async Task<List<PossibleCompound>> GetPossibleCompoundAsync(
        List<WorkoutExerciseRow> originalExercises,
        CancellationToken cancellationToken)
    {
        var originalExerciseTypes = originalExercises.Select(we => we.ExerciseType).Distinct().ToHashSet();

        var compoundExercises = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.Mechanic == "compound" || e.Mechanic == "Compound")
            .ToListAsync(cancellationToken);

        compoundExercises = FilterCompoundExercises(compoundExercises, originalExerciseTypes);

        var compoundIds = compoundExercises.Select(e => e.Id).ToList();

        var compoundSecMuscles = await _dbContext.SecMuscles
            .AsNoTracking()
            .Where(sm => compoundIds.Contains(sm.ExerciseId))
            .ToListAsync(cancellationToken);

        var allMuscles = await _dbContext.Muscles.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

        return compoundExercises.Select(e => new PossibleCompound(
            e,
            allMuscles.TryGetValue(e.PrimaryMuscleId, out var name) ? name : "Unknown",
            compoundSecMuscles
                .Where(sm => sm.ExerciseId == e.Id)
                .Select(sm => sm.MuscleId)
                .Append(e.PrimaryMuscleId)
                .ToList()
        )).ToList();
    }

    private static void AddTopMusclePossibility(
        Guid? topPrimaryMuscleId,
        List<PossibleCompound> possibleCompounds,
        List<WorkoutExerciseRow> selectedCompounds,
        HashSet<Guid> coveredPrimaryMuscles,
        List<Guid> targetMuscleIds,
        int maxExercisesToConsider,
        ref int orderIndex)
    {
        if (!topPrimaryMuscleId.HasValue || selectedCompounds.Count >= maxExercisesToConsider)
        {
            return;
        }

        var topPossible = possibleCompounds
            .Where(c => c.Exercise.PrimaryMuscleId == topPrimaryMuscleId.Value)
            .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
            .FirstOrDefault();

        topPossible ??= possibleCompounds
            .Where(c => c.CoveredMuscles.Contains(topPrimaryMuscleId.Value))
            .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
            .FirstOrDefault();

        if (topPossible != null)
        {
            selectedCompounds.Add(CreateWorkoutExerciseRow(topPossible, orderIndex++));
            coveredPrimaryMuscles.Add(topPrimaryMuscleId.Value);
            possibleCompounds.Remove(topPossible);
        }
    }

    private static void AddDiversePrimaryMusclePossibilities(
        List<Guid> orderedPrimaryMuscleGroups,
        List<PossibleCompound> possibleCompounds,
        List<WorkoutExerciseRow> selectedCompounds,
        HashSet<Guid> coveredPrimaryMuscles,
        List<Guid> targetMuscleIds,
        int maxExercisesToConsider,
        ref int orderIndex)
    {
        foreach (var muscleId in orderedPrimaryMuscleGroups)
        {
            if (selectedCompounds.Count >= maxExercisesToConsider) break;
            if (coveredPrimaryMuscles.Contains(muscleId)) continue;

            var possible = possibleCompounds
                .Where(c => c.Exercise.PrimaryMuscleId == muscleId)
                .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
                .FirstOrDefault();

            possible ??= possibleCompounds
                .Where(c => c.CoveredMuscles.Contains(muscleId))
                .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
                .FirstOrDefault();

            if (possible != null)
            {
                selectedCompounds.Add(CreateWorkoutExerciseRow(possible, orderIndex++));
                coveredPrimaryMuscles.Add(muscleId);
                possibleCompounds.Remove(possible);
            }
        }
    }

    private static void AddRemainingBudgetPossibilities(
        List<PossibleCompound> possibleCompounds,
        List<WorkoutExerciseRow> selectedCompounds,
        List<Guid> targetMuscleIds,
        Dictionary<Guid, int> muscleFrequency,
        int maxExercisesToConsider,
        ref int orderIndex)
    {
        while (possibleCompounds.Count > 0 && selectedCompounds.Count < maxExercisesToConsider)
        {
            var nextPossible = possibleCompounds
                .OrderByDescending(c => c.CoveredMuscles.Where(m => targetMuscleIds.Contains(m)).Sum(m => muscleFrequency.GetValueOrDefault(m, 1)))
                .ThenByDescending(c => muscleFrequency.GetValueOrDefault(c.Exercise.PrimaryMuscleId, 0))
                .FirstOrDefault();

            if (nextPossible == null || !nextPossible.CoveredMuscles.Any(m => targetMuscleIds.Contains(m)))
            {
                break;
            }

            selectedCompounds.Add(CreateWorkoutExerciseRow(nextPossible, orderIndex++));
            possibleCompounds.Remove(nextPossible);
        }
    }

    private static WorkoutExerciseRow CreateWorkoutExerciseRow(PossibleCompound possible, int orderIndex) =>
        new(
            Guid.NewGuid(),
            possible.Exercise.Id,
            orderIndex,
            null,
            possible.Exercise.Name,
            possible.MuscleName,
            possible.Exercise.ExerciseType,
            null,
            null,
            possible.Exercise.ImageUrl,
            possible.Exercise.Equipment);

    private static List<Exercise> FilterCompoundExercises(List<Exercise> compoundExercises, HashSet<ExerciseType> originalExerciseTypes)
    {
        bool isBodyweightOnly = originalExerciseTypes.Count > 0 && originalExerciseTypes.All(t =>
            t == ExerciseType.BodyweightReps ||
            t == ExerciseType.AssistedWeightReps ||
            t == ExerciseType.WeightedBodyweight);

        if (isBodyweightOnly)
        {
            var bodyweightCompounds = compoundExercises.Where(e =>
                e.ExerciseType == ExerciseType.BodyweightReps ||
                e.ExerciseType == ExerciseType.AssistedWeightReps ||
                e.ExerciseType == ExerciseType.WeightedBodyweight).ToList();

            if (bodyweightCompounds.Count > 0)
            {
                return bodyweightCompounds;
            }
        }
        else if (originalExerciseTypes.Count > 0)
        {
            var matchingTypeCompounds = compoundExercises.Where(e => originalExerciseTypes.Contains(e.ExerciseType)).ToList();
            if (matchingTypeCompounds.Count > 0)
            {
                return matchingTypeCompounds;
            }
        }

        return compoundExercises;
    }

    private static int GetBudgetCap(int budgetMinutes) => budgetMinutes switch
    {
        <= 15 => 2,
        <= 30 => 3,
        <= 45 => 5,
        _ => 6
    };

    private async Task SetupTimeConstrainedSetsAsync(
        Guid userId,
        List<WorkoutExerciseRow> workoutExercises,
        Dictionary<Guid, List<WorkoutSetDto>> setsByWorkoutExerciseId,
        int? timeBudgetMinutes,
        CancellationToken cancellationToken)
    {
        var dynamicExerciseIds = workoutExercises
            .Where(we => !setsByWorkoutExerciseId.ContainsKey(we.Id))
            .Select(we => we.ExerciseId)
            .Distinct()
            .ToArray();
        var recentSetsByExId = await GetRecentSetsByExerciseIdAsync(userId, dynamicExerciseIds, cancellationToken);

        int maxSetsPerExercise = timeBudgetMinutes.HasValue && timeBudgetMinutes.Value <= 15 ? 2 : 3;

        foreach (var we in workoutExercises.Where(we => !setsByWorkoutExerciseId.ContainsKey(we.Id)))
        {
            setsByWorkoutExerciseId[we.Id] = BuildSetsForExercise(we.ExerciseId, recentSetsByExId, maxSetsPerExercise);
        }

        foreach (var exerciseId in workoutExercises.Select(we => we.Id))
        {
            if (setsByWorkoutExerciseId.TryGetValue(exerciseId, out var existingSets) && existingSets.Count > maxSetsPerExercise)
            {
                setsByWorkoutExerciseId[exerciseId] = existingSets.Take(maxSetsPerExercise).ToList();
            }
        }

        if (timeBudgetMinutes.HasValue)
        {
            TrimWorkoutToFitBudget(workoutExercises, setsByWorkoutExerciseId, timeBudgetMinutes.Value * 60);
        }
    }

    private static List<WorkoutSetDto> BuildSetsForExercise(
        Guid exerciseId,
        Dictionary<Guid, List<(float? Weight, int? Reps, int? Duration, float? Distance)>> recentSetsByExId,
        int maxSetsPerExercise)
    {
        if (recentSetsByExId.TryGetValue(exerciseId, out var recentSets) && recentSets.Count > 0)
        {
            int orderIndex = 1;
            return recentSets
                .Take(maxSetsPerExercise)
                .Select(rs => new WorkoutSetDto(Guid.NewGuid(), "Normal", rs.Reps, rs.Weight, rs.Duration, rs.Distance, orderIndex++, 90, rs.Weight, rs.Reps))
                .ToList();
        }

        return Enumerable.Range(1, maxSetsPerExercise)
            .Select(s => new WorkoutSetDto(Guid.NewGuid(), "Normal", 10, null, null, null, s, 90))
            .ToList();
    }

    private static void TrimWorkoutToFitBudget(
        List<WorkoutExerciseRow> workoutExercises,
        Dictionary<Guid, List<WorkoutSetDto>> setsByWorkoutExerciseId,
        int maxTimeSeconds)
    {
        int CalculateTotalTime()
        {
            int activeAndRest = workoutExercises.Sum(we =>
                setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets)
                ? sets.Sum(s => ((s.Reps ?? 10) * 4) + s.RestTime)
                : 0);

            int transitionTime = Math.Max(0, workoutExercises.Count - 1) * 60;
            return activeAndRest + transitionTime;
        }

        int totalTimeSeconds = CalculateTotalTime();
        if (totalTimeSeconds <= maxTimeSeconds)
        {
            return;
        }

        totalTimeSeconds = ReduceRestTimes(workoutExercises, setsByWorkoutExerciseId, totalTimeSeconds, maxTimeSeconds, CalculateTotalTime);

        if (totalTimeSeconds > maxTimeSeconds)
        {
            totalTimeSeconds = TrimSets(workoutExercises, setsByWorkoutExerciseId, totalTimeSeconds, maxTimeSeconds, CalculateTotalTime);
        }

        if (totalTimeSeconds > maxTimeSeconds && workoutExercises.Count > 1)
        {
            for (int i = workoutExercises.Count - 1; i >= 1 && totalTimeSeconds > maxTimeSeconds; i--)
            {
                var toRemove = workoutExercises[i];
                workoutExercises.RemoveAt(i);
                setsByWorkoutExerciseId.Remove(toRemove.Id);
                totalTimeSeconds = CalculateTotalTime();
            }
        }
    }

    private static int ReduceRestTimes(
        List<WorkoutExerciseRow> workoutExercises,
        Dictionary<Guid, List<WorkoutSetDto>> setsByWorkoutExerciseId,
        int totalTimeSeconds,
        int maxTimeSeconds,
        Func<int> calculateTotalTime)
    {
        bool reducedRest = true;
        while (reducedRest && totalTimeSeconds > maxTimeSeconds)
        {
            reducedRest = false;
            foreach (var we in workoutExercises)
            {
                if (!setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets)) continue;
                for (int i = 0; i < sets.Count; i++)
                {
                    if (sets[i].RestTime > 60)
                    {
                        sets[i] = sets[i] with { RestTime = Math.Max(60, sets[i].RestTime - 30) };
                        reducedRest = true;
                    }
                }
            }
            totalTimeSeconds = calculateTotalTime();
        }
        return totalTimeSeconds;
    }

    private static int TrimSets(
        List<WorkoutExerciseRow> workoutExercises,
        Dictionary<Guid, List<WorkoutSetDto>> setsByWorkoutExerciseId,
        int totalTimeSeconds,
        int maxTimeSeconds,
        Func<int> calculateTotalTime)
    {
        bool HasTrimmableSets() =>
            workoutExercises.Any(we => setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets) && sets.Count > 1);

        while (totalTimeSeconds > maxTimeSeconds && HasTrimmableSets())
        {
            var ordered = workoutExercises
                .OrderByDescending(we => setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets) ? sets.Count : 0)
                .ToList();

            totalTimeSeconds = DropOneSetFromExercises(ordered, setsByWorkoutExerciseId, maxTimeSeconds, calculateTotalTime);
        }
        return totalTimeSeconds;
    }

    private static int DropOneSetFromExercises(
        List<WorkoutExerciseRow> orderedExercises,
        Dictionary<Guid, List<WorkoutSetDto>> setsByWorkoutExerciseId,
        int maxTimeSeconds,
        Func<int> calculateTotalTime)
    {
        int total = 0;
        foreach (var we in orderedExercises)
        {
            if (setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets) && sets.Count > 1)
            {
                sets.RemoveAt(sets.Count - 1);
                total = calculateTotalTime();
                if (total <= maxTimeSeconds)
                {
                    return total;
                }
            }
        }
        return total > 0 ? total : calculateTotalTime();
    }

    private sealed record PossibleCompound(
        Exercise Exercise,
        string MuscleName,
        List<Guid> CoveredMuscles);
}
