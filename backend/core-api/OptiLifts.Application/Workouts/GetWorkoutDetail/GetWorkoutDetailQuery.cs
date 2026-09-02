using MediatR;

namespace OptiLifts.Application.Workouts.GetWorkoutDetail;

public sealed record GetWorkoutDetailQuery(Guid WorkoutId, Guid UserId, bool IsTimeConstrained = false, int? TimeBudgetMinutes = null) : IRequest<WorkoutDetailDto?>;