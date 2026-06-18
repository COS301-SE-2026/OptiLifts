using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OptiLifts.Application.Exercises.CreateCustomExercise;
using OptiLifts.Application.Storage;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Exercises.CreateCustomExercise;

public class CreateCustomExerciseHandler : IRequestHandler<CreateCustomExerciseCommand, Guid>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public CreateCustomExerciseHandler(OptiLiftsDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    public async Task<Guid> Handle(CreateCustomExerciseCommand request, CancellationToken cancellationToken)
    {
        string? imageUrl = null;

        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName))
        {
            imageUrl = await _blobStorageService.UploadFileAsync(
                request.ImageStream,
                request.ImageFileName,
                request.ImageContentType ?? "application/octet-stream",
                "exercises",
                cancellationToken);
        }

        var exercise = new Exercise
        {
            UserId = request.UserId,
            Name = request.Name,
            Mechanic = request.Mechanic,
            Equipment = request.Equipment,
            Category = request.Category,
            PrimaryMuscles = request.PrimaryMuscles,
            SecondaryMuscles = request.SecondaryMuscles,
            ImageUrl = imageUrl
        };

        _dbContext.Exercises.Add(exercise);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return exercise.Id;
    }
}