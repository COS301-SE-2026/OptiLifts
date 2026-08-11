using MediatR;

namespace OptiLifts.Application.Workouts.DeleteWorkoutLog;

public sealed record DeleteWorkoutLogCommand(Guid WorkoutId, Guid LogId, Guid UserId) : IRequest<bool>;