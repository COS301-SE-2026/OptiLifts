using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.GetExercises;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Exercises.GetExercises;

public class GetExercisesHandler : IRequestHandler<GetExercisesQuery, List<ExerciseDto>>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetExercisesHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ExerciseDto>> Handle(GetExercisesQuery request, CancellationToken cancellationToken)
    {
        var exercises = await _dbContext.Exercises
            .Where(e => e.UserId == null || e.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        var exerciseIds = exercises.Select(e => e.Id).ToList();
        var primaryMuscleIds = exercises.Select(e => e.PrimaryMuscleId).Distinct().ToList();

        var muscleNamesById = await _dbContext.Muscles
            .Where(m => primaryMuscleIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

        var secondaryMuscles = await (from secondary in _dbContext.SecMuscles
                                      join muscle in _dbContext.Muscles on secondary.MuscleId equals muscle.Id
                                      where exerciseIds.Contains(secondary.ExerciseId)
                                      select new { secondary.ExerciseId, MuscleName = muscle.Name })
            .ToListAsync(cancellationToken);

        var secondaryMusclesByExerciseId = secondaryMuscles
            .GroupBy(entry => entry.ExerciseId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.MuscleName).Distinct().ToList());

        return exercises.Select(e => new ExerciseDto(
            e.Id,
            e.Name,
            e.Mechanic,
            e.Equipment,
            ToFrontendExerciseType(e.ExerciseType),
            muscleNamesById.TryGetValue(e.PrimaryMuscleId, out var primaryMuscleName)
                ? [primaryMuscleName]
                : [],
            secondaryMusclesByExerciseId.TryGetValue(e.Id, out var secondaryMuscleNames)
                ? secondaryMuscleNames
                : [],
            e.UserId != null,
            e.ImageUrl
        )).ToList();
    }

    private static string ToFrontendExerciseType(ExerciseType exerciseType)
    {
        return exerciseType switch
        {
            ExerciseType.WeightReps => "weight-reps",
            ExerciseType.BodyweightReps => "bodyweight-reps",
            ExerciseType.AssistedWeightReps => "assisted-bodyweight",
            ExerciseType.WeightedBodyweight => "weighted-bodyweight",
            ExerciseType.Duration => "duration",
            ExerciseType.DurationWeight => "duration-weight",
            ExerciseType.DistanceDuration => "distance-duration",
            ExerciseType.WeightDistance => "weight-distance",
            _ => exerciseType.ToString()
        };
    }
}