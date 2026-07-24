using MediatR;
using OptiLifts.Application.Exercises.GetExercises;

namespace OptiLifts.Application.Exercises.GetExerciseById;

public sealed record GetExerciseByIdQuery(Guid ExerciseId, Guid UserId) : IRequest<ExerciseDto?>;
