using MediatR;

namespace OptiLifts.Application.Workouts.GetWorkoutDetail;

public sealed record GetWorkoutDetailQuery(Guid WorkoutId, Guid UserId) : IRequest<WorkoutDetailDto?>;