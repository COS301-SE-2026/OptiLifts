using MediatR;

namespace OptiLifts.Application.Exercises.DeleteCustomExercise;

public sealed record DeleteCustomExerciseCommand(Guid ExerciseId, Guid UserId) : IRequest<bool>;