using MediatR;

namespace OptiLifts.Application.Exercises.GetExercises;

public record GetExercisesQuery(
    Guid UserId,
    string? Search = null,
    string? Muscle = null,
    string? Equipment = null,
    bool CustomOnly = false
    ) : IRequest<List<ExerciseDto>>;
