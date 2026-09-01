using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class UpdateUserRepRangeHandler : IRequestHandler<UpdateUserRepRangeCommand>
{
    private readonly OptiLiftsDbContext _dbContext;

    public UpdateUserRepRangeHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateUserRepRangeCommand request, CancellationToken cancellationToken)
    {
        var repRange = await _dbContext.UserRepRanges
            .FirstOrDefaultAsync(r => r.Id == request.RepRangeId && r.UserId == request.UserId, cancellationToken);

        if (repRange == null)
        {
            throw new KeyNotFoundException("Rep range not found.");
        }

        var duplicateTypeExists = await _dbContext.UserRepRanges
            .AsNoTracking()
            .AnyAsync(
                r => r.UserId == request.UserId
                    && r.Id != request.RepRangeId
                    && r.ExerciseType == request.ExerciseType,
                cancellationToken);

        if (duplicateTypeExists)
        {
            throw new ArgumentException("A rep range for this exercise type already exists.");
        }

        repRange.ExerciseType = request.ExerciseType;
        repRange.LowerLimit = request.LowerLimit;
        repRange.UpperLimit = request.UpperLimit;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
