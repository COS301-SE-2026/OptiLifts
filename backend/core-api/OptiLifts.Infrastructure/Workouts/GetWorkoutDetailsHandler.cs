using System.Net.NetworkInformation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutDetails;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutDetailsHandler : IRequestHandler<GetWorkoutDetailsQuery, WorkoutDetailsDto?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutDetailsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<WorkoutDetailsDto?> Handle(GetWorkoutDetailsQuery request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId, cancellationToken);

        if (workout == null)
        {
            return null;
        }

        var exercises = await (
            from we in _dbContext.WorkoutExercises.AsNoTracking()
            where we.WorkoutId == workout.Id
            join ex in _dbContext.Exercises.AsNoTracking() on we.ExerciseId equals ex.Id
            select new
            {
                we.Id,
                ExerciseCatalogId = ex.Id,
                ex.Name,
                ex.ImageUrl,
                ex.PrimaryMuscleId,
                we.OrderIndex
            })
            .OrderBy(e => e.OrderIndex)
            .ToListAsync(cancellationToken);

        var muscleIds = exercises.Select(e => e.PrimaryMuscleId).Distinct().ToArray();
        var muscleMap = await _dbContext.Muscles
            .AsNoTracking()
            .Where(m => muscleIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

        var exerciseIds = exercises.Select(e => e.Id).ToArray();
        var sets = await _dbContext.Sets
            .AsNoTracking()
            .Where(s => exerciseIds.Contains(s.WorkoutExerciseId))
            .OrderBy(s => s.OrderIndex)
            .ToListAsync(cancellationToken);

        var mapped = exercises.Select(ex =>
        {
            var exerciseSets = sets
                .Where(s => s.WorkoutExerciseId == ex.Id)
                .Select(s => new WorkoutDetailsSetDto(
                    s.Id,
                    MapSetTypeToFrontend(s.Type),
                    s.Weight,
                    s.Reps,
                    s.OrderIndex
                ))
                .ToList();

            var muscleName = muscleMap.TryGetValue(ex.PrimaryMuscleId, out var name) ? name : "Other";
            return new WorkoutDetailsExerciseDto(
                ex.Id,
                ex.ExerciseCatalogId,
                ex.Name,
                muscleName,
                ex.ImageUrl,
                exerciseSets,
                ex.OrderIndex
            );
        }).ToList();

        return new WorkoutDetailsDto(
            workout.Id,
            workout.Name,
            workout.FolderId,
            mapped
        );
    }
    private static string MapSetTypeToFrontend(Domain.Workouts.SetType type) => type switch
    {
        Domain.Workouts.SetType.Warmup => "W",
        Domain.Workouts.SetType.DropSet => "D",
        _ => "I"
    };
}