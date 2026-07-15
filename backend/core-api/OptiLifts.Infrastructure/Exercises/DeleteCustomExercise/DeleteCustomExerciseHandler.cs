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
    private readonly IBlobStorageService _blobStorageService;

    public DeleteCustomExerciseHandler(OptiLiftsDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    public async Task<bool> Handle(DeleteCustomExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == request.ExerciseId && e.UserId == request.UserId, cancellationToken);

        if (exercise == null)
            return false;

        if (!string.IsNullOrWhiteSpace(exercise.ImageUrl))
            await _blobStorageService.DeleteFileAsync(exercise.ImageUrl, ExerciseContainerName, cancellationToken);

        var secondaryMuscles = await _dbContext.SecMuscles.Where(sm => sm.ExerciseId == exercise.Id).ToListAsync(cancellationToken);

        if (secondaryMuscles.Count > 0)
            _dbContext.SecMuscles.RemoveRange(secondaryMuscles);

        _dbContext.Exercises.Remove(exercise);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}