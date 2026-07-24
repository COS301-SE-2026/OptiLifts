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
        var query = _dbContext.Exercises
            .Where(e => !e.IsDeleted && (e.UserId == null || e.UserId == request.UserId));

        if (!string.IsNullOrWhiteSpace(request.Equipment))
        {
            var equipment = request.Equipment.Trim().ToLower();
            query = query.Where(e => e.Equipment != null && e.Equipment.ToLower() == equipment);
        }

        if (!string.IsNullOrWhiteSpace(request.Muscle))
        {
            var muscle = request.Muscle.Trim().ToLower();
            query = query.Where(e => _dbContext.Muscles.Any(m => m.Id == e.PrimaryMuscleId && m.Name.ToLower() == muscle));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(search) || _dbContext.Muscles.Any(m => m.Id == e.PrimaryMuscleId && m.Name.ToLower().Contains(search)));
        }

        var exercises = await query.ToListAsync(cancellationToken);

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
            ExerciseType.WeightReps => "WeightReps",
            ExerciseType.BodyweightReps => "BodyweightReps",
            ExerciseType.AssistedWeightReps => "AssistedWeightReps",
            ExerciseType.WeightedBodyweight => "WeightedBodyWeight",
            ExerciseType.Duration => "Duration",
            ExerciseType.DurationWeight => "DurationWeight",
            ExerciseType.DistanceDuration => "DistanceDuration",
            ExerciseType.WeightDistance => "WeightDistance",
            _ => exerciseType.ToString()
        };
    }
}