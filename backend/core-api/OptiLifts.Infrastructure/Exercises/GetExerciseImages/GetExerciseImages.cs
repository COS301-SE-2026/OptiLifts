using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.GetExerciseImages;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Exercises.GetExerciseImages;

public sealed class GetExerciseImagesHandler : IRequestHandler<GetExerciseImagesQuery, Dictionary<string, string>>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetExerciseImagesHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, string>> Handle(GetExerciseImagesQuery request, CancellationToken cancellationToken)
    {
        var images = await _dbContext.Exercises
            .AsNoTracking()
            .Where(exc => request.ExerciseNames.Contains(exc.Name) && exc.ImageUrl != null)
            .ToDictionaryAsync(exc => exc.Name, exc => exc.ImageUrl!, cancellationToken);
        return images;
    }
}