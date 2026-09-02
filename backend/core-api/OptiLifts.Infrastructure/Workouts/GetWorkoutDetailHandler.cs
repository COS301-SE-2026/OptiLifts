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
            .AsNoTracking()
            .FirstOrDefaultAsync(workout => workout.Id == request.WorkoutId && workout.CreatedBy == request.UserId && !workout.IsDeleted, cancellationToken);

        if (workout is null)
        {
            return null;
        }

        var workoutExercises = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == workout.Id
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

        if (request.IsTimeConstrained && workoutExercises.Count > 0)
        {
            var originalExerciseCount = workoutExercises.Count;

            var originalPrimaryMuscles = await _dbContext.WorkoutExercises
                .Where(we => we.WorkoutId == workout.Id)
                .Join(_dbContext.Exercises, we => we.ExerciseId, e => e.Id, (we, e) => e.PrimaryMuscleId)
                .ToListAsync(cancellationToken);

            var originalSecondaryMuscles = await _dbContext.WorkoutExercises
                .Where(we => we.WorkoutId == workout.Id)
                .Join(_dbContext.SecMuscles, we => we.ExerciseId, sm => sm.ExerciseId, (we, sm) => sm.MuscleId)
                .ToListAsync(cancellationToken);

            // Calculate muscle group priority based on frequency in original workout:
            var primaryMuscleCounts = originalPrimaryMuscles
                .GroupBy(m => m)
                .ToDictionary(g => g.Key, g => g.Count());

            var orderedPrimaryMuscleGroups = originalPrimaryMuscles
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            var topPrimaryMuscleId = orderedPrimaryMuscleGroups.Count > 0 ? (Guid?)orderedPrimaryMuscleGroups[0] : null;

            // Primary muscle targets weighted 3x, secondary targets weighted 1x
            var muscleFrequency = new Dictionary<Guid, int>();
            foreach (var m in originalPrimaryMuscles)
            {
                muscleFrequency[m] = muscleFrequency.GetValueOrDefault(m, 0) + 3;
            }
            foreach (var m in originalSecondaryMuscles)
            {
                muscleFrequency[m] = muscleFrequency.GetValueOrDefault(m, 0) + 1;
            }

            var targetMuscleIds = muscleFrequency
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            var originalExerciseTypes = workoutExercises.Select(we => we.ExerciseType).Distinct().ToHashSet();
            bool isBodyweightOnly = originalExerciseTypes.Count > 0 && originalExerciseTypes.All(t =>
                t == ExerciseType.BodyweightReps ||
                t == ExerciseType.AssistedWeightReps ||
                t == ExerciseType.WeightedBodyweight);

            var compoundExercises = await _dbContext.Exercises
                .AsNoTracking()
                .Where(e => e.Mechanic == "compound" || e.Mechanic == "Compound")
                .ToListAsync(cancellationToken);

            if (isBodyweightOnly)
            {
                var bodyweightCompounds = compoundExercises.Where(e =>
                    e.ExerciseType == ExerciseType.BodyweightReps ||
                    e.ExerciseType == ExerciseType.AssistedWeightReps ||
                    e.ExerciseType == ExerciseType.WeightedBodyweight).ToList();

                if (bodyweightCompounds.Count > 0)
                {
                    compoundExercises = bodyweightCompounds;
                }
            }
            else if (originalExerciseTypes.Count > 0)
            {
                var matchingTypeCompounds = compoundExercises.Where(e => originalExerciseTypes.Contains(e.ExerciseType)).ToList();
                if (matchingTypeCompounds.Count > 0)
                {
                    compoundExercises = matchingTypeCompounds;
                }
            }

            var compoundIds = compoundExercises.Select(e => e.Id).ToList();

            var compoundSecMuscles = await _dbContext.SecMuscles
                .AsNoTracking()
                .Where(sm => compoundIds.Contains(sm.ExerciseId))
                .ToListAsync(cancellationToken);

            var allMuscles = await _dbContext.Muscles.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

            var compoundCandidates = compoundExercises.Select(e => new
            {
                Exercise = e,
                MuscleName = allMuscles.TryGetValue(e.PrimaryMuscleId, out var name) ? name : "Unknown",
                CoveredMuscles = compoundSecMuscles
                    .Where(sm => sm.ExerciseId == e.Id)
                    .Select(sm => sm.MuscleId)
                    .Append(e.PrimaryMuscleId)
                    .ToList()
            }).ToList();

            var selectedCompounds = new List<WorkoutExerciseRow>();
            var coveredPrimaryMuscles = new HashSet<Guid>();
            int orderIndex = 0;
            int maxExercisesToConsider;
            if (request.TimeBudgetMinutes.HasValue)
            {
                int budget = request.TimeBudgetMinutes.Value;
                int budgetCap = budget switch
                {
                    <= 15 => 2,
                    <= 30 => 3,
                    <= 45 => 5,
                    _ => 6
                };
                maxExercisesToConsider = Math.Min(originalExerciseCount, budgetCap);
            }
            else
            {
                maxExercisesToConsider = originalExerciseCount;
            }

            // Step 1: Ensure the most targeted muscle group from the original workout is present
            if (topPrimaryMuscleId.HasValue && selectedCompounds.Count < maxExercisesToConsider)
            {
                var topCandidate = compoundCandidates
                    .Where(c => c.Exercise.PrimaryMuscleId == topPrimaryMuscleId.Value)
                    .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
                    .FirstOrDefault();

                topCandidate ??= compoundCandidates
                    .Where(c => c.CoveredMuscles.Contains(topPrimaryMuscleId.Value))
                    .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
                    .FirstOrDefault();

                if (topCandidate != null)
                {
                    selectedCompounds.Add(new WorkoutExerciseRow(
                        Guid.NewGuid(),
                        topCandidate.Exercise.Id,
                        orderIndex++,
                        null,
                        topCandidate.Exercise.Name,
                        topCandidate.MuscleName,
                        topCandidate.Exercise.ExerciseType,
                        null,
                        null,
                        topCandidate.Exercise.ImageUrl,
                        topCandidate.Exercise.Equipment
                    ));
                    coveredPrimaryMuscles.Add(topPrimaryMuscleId.Value);
                    compoundCandidates.Remove(topCandidate);
                }
            }

            // Step 2: Include the other muscle groups from the original workout that can fit in the budget
            foreach (var muscleId in orderedPrimaryMuscleGroups)
            {
                if (selectedCompounds.Count >= maxExercisesToConsider)
                {
                    break;
                }

                if (coveredPrimaryMuscles.Contains(muscleId))
                {
                    continue;
                }

                var candidate = compoundCandidates
                    .Where(c => c.Exercise.PrimaryMuscleId == muscleId)
                    .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
                    .FirstOrDefault();

                candidate ??= compoundCandidates
                    .Where(c => c.CoveredMuscles.Contains(muscleId))
                    .OrderByDescending(c => c.CoveredMuscles.Count(m => targetMuscleIds.Contains(m)))
                    .FirstOrDefault();

                if (candidate != null)
                {
                    selectedCompounds.Add(new WorkoutExerciseRow(
                        Guid.NewGuid(),
                        candidate.Exercise.Id,
                        orderIndex++,
                        null,
                        candidate.Exercise.Name,
                        candidate.MuscleName,
                        candidate.Exercise.ExerciseType,
                        null,
                        null,
                        candidate.Exercise.ImageUrl,
                        candidate.Exercise.Equipment
                    ));
                    coveredPrimaryMuscles.Add(muscleId);
                    compoundCandidates.Remove(candidate);
                }
            }

            // Step 3: If all muscle groups are represented and budget allows more, add additional compound movements
            while (compoundCandidates.Count > 0 && selectedCompounds.Count < maxExercisesToConsider)
            {
                var nextCandidate = compoundCandidates
                    .OrderByDescending(c => c.CoveredMuscles.Where(m => targetMuscleIds.Contains(m)).Sum(m => muscleFrequency.GetValueOrDefault(m, 1)))
                    .ThenByDescending(c => muscleFrequency.GetValueOrDefault(c.Exercise.PrimaryMuscleId, 0))
                    .FirstOrDefault();

                if (nextCandidate == null || !nextCandidate.CoveredMuscles.Any(m => targetMuscleIds.Contains(m)))
                {
                    break;
                }

                selectedCompounds.Add(new WorkoutExerciseRow(
                    Guid.NewGuid(),
                    nextCandidate.Exercise.Id,
                    orderIndex++,
                    null,
                    nextCandidate.Exercise.Name,
                    nextCandidate.MuscleName,
                    nextCandidate.Exercise.ExerciseType,
                    null,
                    null,
                    nextCandidate.Exercise.ImageUrl,
                    nextCandidate.Exercise.Equipment
                ));

                compoundCandidates.Remove(nextCandidate);
            }

            workoutExercises = selectedCompounds;
        }

        var exerId = workoutExercises.Select(entry => entry.ExerciseId).Distinct().ToArray();

        var bestVal = await _dbContext.ExercisePrs
            .AsNoTracking()
            .Where(pr => pr.UserId == request.UserId && exerId.Contains(pr.ExerciseId))
            .GroupBy(pr => new { pr.ExerciseId, pr.PrType })
            .Select(group => new { group.Key.ExerciseId, group.Key.PrType, Best = group.Max(pr => pr.PrValue) })
            .ToListAsync(cancellationToken);

        var bestWeightByExer = bestVal
            .Where(item => item.PrType == ExercisePrType.MaxWeight)
            .ToDictionary(item => item.ExerciseId, item => item.Best);

        var bestVolByExer = bestVal
            .Where(item => item.PrType == ExercisePrType.MaxSetVolume)
            .ToDictionary(item => item.ExerciseId, item => item.Best);

        var estimationsByExerciseId = (await _dbContext.ExerciseEstimations
            .AsNoTracking()
            .Where(estimation => estimation.UserId == request.UserId && exerId.Contains(estimation.ExerciseId))
            .OrderByDescending(estimation => estimation.TimeStamp)
            .ToListAsync(cancellationToken))
            .GroupBy(estimation => estimation.ExerciseId)
            .ToDictionary(group => group.Key, group => group.First());

        var secondaryMuscleRows = await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutExercise.WorkoutId == workout.Id
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

        var secondaryMusclesByExerciseId = secondaryMuscleRows
            .GroupBy(entry => entry.ExerciseId)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Name).Distinct().ToArray());

        var workoutExerciseIds = workoutExercises.Select(entry => entry.Id).ToArray();
        var previousByWorkoutExerciseId = await GetPreviousPerformanceByWorkoutExerciseIdAsync(request.UserId, workoutExerciseIds, cancellationToken);
        var setsByWorkoutExerciseId = await GetSetsByWorkoutExerciseIdAsync(workoutExerciseIds, previousByWorkoutExerciseId, cancellationToken);

        if (request.IsTimeConstrained)
        {
            var dynamicExerciseIds = workoutExercises.Where(we => !setsByWorkoutExerciseId.ContainsKey(we.Id)).Select(we => we.ExerciseId).Distinct().ToArray();
            var recentSetsByExId = await GetRecentSetsByExerciseIdAsync(request.UserId, dynamicExerciseIds, cancellationToken);

            int maxSetsPerExercise = request.TimeBudgetMinutes.HasValue && request.TimeBudgetMinutes.Value <= 15 ? 2 : 3;

            foreach (var we in workoutExercises.Where(we => !setsByWorkoutExerciseId.ContainsKey(we.Id)))
            {
                if (recentSetsByExId.TryGetValue(we.ExerciseId, out var recentSets) && recentSets.Count > 0)
                {
                    var newSets = new List<WorkoutSetDto>();
                    int orderIndex = 1;
                    foreach (var rs in recentSets.Take(maxSetsPerExercise))
                    {
                        newSets.Add(new WorkoutSetDto(Guid.NewGuid(), "Normal", rs.Reps, rs.Weight, rs.Duration, rs.Distance, orderIndex++, 90, rs.Weight, rs.Reps));
                    }
                    setsByWorkoutExerciseId[we.Id] = newSets;
                }
                else
                {
                    var defaultSets = new List<WorkoutSetDto>();
                    for (int s = 1; s <= maxSetsPerExercise; s++)
                    {
                        defaultSets.Add(new WorkoutSetDto(Guid.NewGuid(), "Normal", 10, null, null, null, s, 90));
                    }
                    setsByWorkoutExerciseId[we.Id] = defaultSets;
                }
            }

            foreach (var we in workoutExercises)
            {
                if (setsByWorkoutExerciseId.TryGetValue(we.Id, out var existingSets) && existingSets.Count > maxSetsPerExercise)
                {
                    setsByWorkoutExerciseId[we.Id] = existingSets.Take(maxSetsPerExercise).ToList();
                }
            }

            if (request.TimeBudgetMinutes.HasValue)
            {
                int maxTimeSeconds = request.TimeBudgetMinutes.Value * 60;

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

                if (totalTimeSeconds > maxTimeSeconds)
                {
                    // 1. Reduce Rest Times: Shave 30s off rest periods down to 60s
                    bool reducedRest = true;
                    while (reducedRest && totalTimeSeconds > maxTimeSeconds)
                    {
                        reducedRest = false;
                        foreach (var we in workoutExercises)
                        {
                            if (setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets))
                            {
                                for (int i = 0; i < sets.Count; i++)
                                {
                                    if (sets[i].RestTime > 60)
                                    {
                                        sets[i] = sets[i] with { RestTime = Math.Max(60, sets[i].RestTime - 30) };
                                        reducedRest = true;
                                    }
                                }
                            }
                        }
                        totalTimeSeconds = CalculateTotalTime();
                    }

                    // 2. Trim Sets: Drop total number of sets for exercises from 3 down to 2, then down to 1
                    if (totalTimeSeconds > maxTimeSeconds)
                    {
                        var orderedExercises = workoutExercises.OrderByDescending(we =>
                            setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets) ? sets.Count : 0).ToList();

                        while (totalTimeSeconds > maxTimeSeconds && orderedExercises.Any(we => setsByWorkoutExerciseId.ContainsKey(we.Id) && setsByWorkoutExerciseId[we.Id].Count > 1))
                        {
                            foreach (var we in orderedExercises)
                            {
                                if (setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets) && sets.Count > 1)
                                {
                                    sets.RemoveAt(sets.Count - 1);
                                    totalTimeSeconds = CalculateTotalTime();
                                    if (totalTimeSeconds <= maxTimeSeconds) break;
                                }
                            }
                            orderedExercises = orderedExercises.OrderByDescending(we =>
                                setsByWorkoutExerciseId.TryGetValue(we.Id, out var sets) ? sets.Count : 0).ToList();
                        }
                    }

                    // 3. Prune the Small Stuff: If still over budget, drop secondary compound exercises to protect core movements
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
            }
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
                .Select(row => ((float?)row.Weight, (int?)row.Reps, (int?)row.Duration, (float?)row.Distance))
                .ToList());
    }


}
