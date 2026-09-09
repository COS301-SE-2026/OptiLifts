using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.GetExerciseById;
using OptiLifts.Application.Exercises.GetExercises;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Exercises.GetExerciseById;

public sealed class GetExerciseByIdHandler : IRequestHandler<GetExerciseByIdQuery, ExerciseDto?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetExerciseByIdHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExerciseDto?> Handle(GetExerciseByIdQuery request, CancellationToken cancellationToken)
    {
        var ex = await _dbContext.Exercises.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.ExerciseId && (e.UserId == null ? !e.IsDeleted : e.UserId == request.UserId), cancellationToken);

        if (ex == null)
            return null;

        var primMuscle = await _dbContext.Muscles
            .Where(m => m.Id == ex.PrimaryMuscleId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var secMuscle = await (from secondary in _dbContext.SecMuscles
                               join muscle in _dbContext.Muscles on secondary.MuscleId equals muscle.Id
                               where secondary.ExerciseId == ex.Id
                               select muscle.Name).Distinct().ToListAsync(cancellationToken);

        var prs = await _dbContext.ExercisePrs
            .AsNoTracking()
            .Where(pr => pr.UserId == request.UserId && pr.ExerciseId == ex.Id)
            .GroupBy(pr => pr.PrType)
            .Select(group => new { PrType = group.Key, Best = group.Max(pr => pr.PrValue) })
            .ToListAsync(cancellationToken);

        float? BestOf(ExercisePrType type) =>
            prs.Where(pr => pr.PrType == type).Select(pr => (float?)pr.Best).FirstOrDefault();


        return new ExerciseDto(
            ex.Id,
            ex.Name,
            ex.Mechanic,
            ex.Equipment,
            ex.ExerciseType.ToString(),
            primMuscle is null ? [] : [primMuscle],
            secMuscle,
            ex.UserId != null,
            ex.ImageUrl,
            ex.IsDeleted,
            BestOf(ExercisePrType.MaxWeight),
            BestOf(ExercisePrType.MaxSetVolume)
        );
    }
}
