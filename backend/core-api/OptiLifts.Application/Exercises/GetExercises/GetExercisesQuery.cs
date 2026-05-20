using MediatR;

namespace OptiLifts.Application.Exercises.GetExercises;

public record GetExercisesQuery(Guid UserId) : IRequest<List<ExerciseDto>>;
