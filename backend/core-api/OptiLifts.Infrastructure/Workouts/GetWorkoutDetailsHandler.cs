using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.GetWorkoutDetails;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class GetWorkoutDetailsHandler : IRequestHandler<GetWorkoutDetailsQuery, WorkoutDetailDto?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetWorkoutDetailsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutDetailDto?> Handle(GetWorkoutDetailsQuery request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .AsNoTracking()
            .Where(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId)
            .Select(w => new { w.Id, w.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (workout is null)
        {
            return null;
        }

    }
}
