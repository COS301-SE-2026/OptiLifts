using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Exercises.CreateCustomExercise;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Exercises.CreateCustomExercise;

public class CreateCustomExerciseHandler : IRequestHandler<CreateCustomExerciseCommand, Guid>
{
    private readonly OptiLiftsDbContext _dbContext;

    public CreateCustomExerciseHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateCustomExerciseCommand request, CancellationToken cancellationToken)
    {
        var exerciseType = ResolveExerciseType(request.Category);
        var primaryMuscleId = await ResolveMuscleIdAsync(request.PrimaryMuscles, "primary muscle", cancellationToken);
        var secondaryMuscleIds = await ResolveMuscleIdsAsync(request.SecondaryMuscles, primaryMuscleId, cancellationToken);

        string? imageUrl = null;

        if (request.ImageStream != null && !string.IsNullOrEmpty(request.ImageFileName))
        {
            imageUrl = await BuildImageDataUrlAsync(request.ImageStream, request.ImageContentType ?? "application/octet-stream", cancellationToken);
        }

        var exercise = new Exercise
        {
            UserId = request.UserId,
            Name = request.Name,
            Mechanic = request.Mechanic,
            Equipment = request.Equipment?.Trim().ToLower(),
            ExerciseType = exerciseType,
            PrimaryMuscleId = primaryMuscleId,
            ImageUrl = imageUrl
        };

        _dbContext.Exercises.Add(exercise);

        foreach (var secondaryMuscleId in secondaryMuscleIds)
        {
            _dbContext.SecMuscles.Add(new SecMuscle
            {
                ExerciseId = exercise.Id,
                MuscleId = secondaryMuscleId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return exercise.Id;
    }

    private static async Task<string> BuildImageDataUrlAsync(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var base64 = Convert.ToBase64String(memoryStream.ToArray());
        return $"data:{contentType};base64,{base64}";
    }

    private async Task<Guid> ResolveMuscleIdAsync(IEnumerable<string> values, string fieldName, CancellationToken cancellationToken)
    {
        var value = values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"A {fieldName} is required.");

        if (Guid.TryParse(value, out var muscleId))
        {
            var exists = await _dbContext.Muscles.AnyAsync(m => m.Id == muscleId, cancellationToken);
            if (exists)
                return muscleId;
        }

        var muscle = await _dbContext.Muscles.FirstOrDefaultAsync(m => m.Name == value, cancellationToken);
        if (muscle is null)
            throw new InvalidOperationException($"Unknown {fieldName}: {value}");

        return muscle.Id;
    }

    private async Task<List<Guid>> ResolveMuscleIdsAsync(IEnumerable<string> values, Guid primaryMuscleId, CancellationToken cancellationToken)
    {
        var resolved = new List<Guid>();

        foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(value, out var muscleId))
            {
                if (muscleId == primaryMuscleId)
                    continue;

                var exists = await _dbContext.Muscles.AnyAsync(m => m.Id == muscleId, cancellationToken);
                if (exists)
                {
                    resolved.Add(muscleId);
                    continue;
                }
            }

            var muscle = await _dbContext.Muscles.FirstOrDefaultAsync(m => m.Name == value, cancellationToken);
            if (muscle is null || muscle.Id == primaryMuscleId)
                continue;

            resolved.Add(muscle.Id);
        }

        return resolved;
    }

    private static ExerciseType ResolveExerciseType(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "strength" or "weight-reps" => ExerciseType.WeightReps,
            "bodyweight-reps" => ExerciseType.BodyweightReps,
            "weighted-bodyweight" => ExerciseType.WeightedBodyweight,
            "assisted-bodyweight" => ExerciseType.AssistedWeightReps,
            "duration" => ExerciseType.Duration,
            "duration-weight" => ExerciseType.DurationWeight,
            "distance-duration" => ExerciseType.DistanceDuration,
            "weight-distance" => ExerciseType.WeightDistance,
            _ => throw new InvalidOperationException($"Unsupported exercise type: {value}")
        };
    }
}