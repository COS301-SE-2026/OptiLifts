using MediatR;

namespace OptiLifts.Application.Workouts.ReplaceWorkoutExercise;

public sealed record ReplaceWorkoutExerciseCommand(
    Guid UserId,
    Guid WorkoutId,
    Guid OldExerciseId,
    Guid NewExerciseId) : IRequest<bool>;
