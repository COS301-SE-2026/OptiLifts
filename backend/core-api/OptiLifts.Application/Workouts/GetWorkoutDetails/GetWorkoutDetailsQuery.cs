using MediatR;

namespace OptiLifts.Application.Workouts.GetWorkoutDetails;

public sealed record GetWorkoutDetailsQuery(Guid WorkoutId, Guid UserId) : IRequest<WorkoutDetailsDto?>;
