using MediatR;

namespace OptiLifts.Application.Workouts.GetWorkoutLogDetail;

public sealed record GetWorkoutLogDetailQuery(Guid WorkoutId, Guid LogId, Guid UserId) : IRequest<WorkoutLogDetailDto?>;