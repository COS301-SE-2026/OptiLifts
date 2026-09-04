using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.UpdateCustomExercise;
using OptiLifts.Application.Storage;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Exercises.UpdateCustomExercise;

public sealed class UpdateCustomExerciseHandler : IRequestHandler<UpdateCustomExerciseCommand, bool>
{
    private const string ExerciseContainerName = "exercises";

    private readonly OptiLiftsDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public UpdateCustomExerciseHandler(OptiLiftsDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    public async Task<bool> Handle(UpdateCustomExerciseCommand request, CancellationToken cancellationToken)
    {
        var ex = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == request.ExerciseId && e.UserId == request.UserId && !e.IsDeleted, cancellationToken);

        if (ex == null)
        {
            return false;
        }

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Exercise name is required.");
        }

        var nameExists = await _dbContext.Exercises.AnyAsync(
            e => e.Id != request.ExerciseId &&
                 !e.IsDeleted &&
                 (e.UserId == null || e.UserId == request.UserId) &&
                 e.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException($"An exercise with the name '{name}' already exists.");
        }

        ex.Name = name;

        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName))
        {
            var newUrl = await _blobStorageService.UploadFileAsync(request.ImageStream, request.ImageFileName,
                request.ImageContentType ?? "application/octet-stream", ExerciseContainerName, cancellationToken);

            if (!string.IsNullOrWhiteSpace(ex.ImageUrl))
            {
                await _blobStorageService.DeleteFileAsync(ex.ImageUrl, ExerciseContainerName, cancellationToken);
            }

            ex.ImageUrl = newUrl;
        }
        else if (request.RemoveImage && !string.IsNullOrWhiteSpace(ex.ImageUrl))
        {
            await _blobStorageService.DeleteFileAsync(ex.ImageUrl, ExerciseContainerName, cancellationToken);
            ex.ImageUrl = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
