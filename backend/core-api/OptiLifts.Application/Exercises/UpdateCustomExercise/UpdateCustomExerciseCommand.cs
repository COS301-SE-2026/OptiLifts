using System.IO;
using MediatR;

namespace OptiLifts.Application.Exercises.UpdateCustomExercise;

public sealed record UpdateCustomExerciseCommand(
    Guid ExerciseId,
    Guid UserId,
    string Name,
    Stream? ImageStream,
    string? ImageFileName,
    string? ImageContentType,
    bool RemoveImage
) : IRequest<bool>;