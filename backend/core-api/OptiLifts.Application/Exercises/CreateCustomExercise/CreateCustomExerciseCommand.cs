using MediatR;
using System.IO;

namespace OptiLifts.Application.Exercises.CreateCustomExercise;

public record CreateCustomExerciseCommand(
    Guid UserId,
    string Name,
    string? Mechanic,
    string? Equipment,
    string Category,
    List<string> PrimaryMuscles,
    List<string> SecondaryMuscles,
    Stream? ImageStream,
    string? ImageFileName,
    string? ImageContentType
) : IRequest<Guid>;
