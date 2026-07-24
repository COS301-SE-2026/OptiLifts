using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.DeleteCustomExercise;
using OptiLifts.Application.Storage;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Exercises.DeleteCustomExercise;

public sealed class DeleteCustomExerciseHandler : IRequestHandler<DeleteCustomExerciseCommand, bool>
{
    private const string ExerciseContainerName = "exercises";

    private readonly OptiLiftsDbContext _dbContext;

    public DeleteCustomExerciseHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteCustomExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == request.ExerciseId && e.UserId == request.UserId, cancellationToken);

        if (exercise == null)
            return false;

        exercise.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}