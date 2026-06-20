using MediatR;

namespace OptiLifts.Application.Workouts.DeleteWorkout;

public sealed record DeleteWorkoutCommand(Guid WorkoutId, Guid UserId) : IRequest<bool>;