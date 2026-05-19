using MediatR;

namespace OptiLifts.Application.Workouts.AddExerciseToWorkout;

public sealed record AddExerciseToWorkoutCommand(
    Guid WorkoutId,
    Guid UserId,
    Guid ExerciseId) : IRequest<bool>;