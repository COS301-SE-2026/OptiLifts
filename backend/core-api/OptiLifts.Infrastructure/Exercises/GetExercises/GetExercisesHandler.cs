using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.GetExercises;
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

        return exercises.Select(e => new ExerciseDto(
            e.Id,
            e.Name,
            e.Mechanic,
            e.Equipment,
            e.Category,
            e.PrimaryMuscles,
            e.SecondaryMuscles,
            e.UserId != null
        )).ToList();
    }
}