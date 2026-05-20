using MediatR;

namespace OptiLifts.Application.Exercises.CreateCustomExercise;

public record CreateCustomExerciseCommand(
    Guid UserId,
    string Name,
    string? Mechanic,
    string? Equipment,
    string Category,
    List<string> PrimaryMuscles,
    List<string> SecondaryMuscles
) : IRequest<Guid>;
