using MediatR;

namespace OptiLifts.Application.Workouts.GetWorkouts;

//the request model (ie data needed to run query)
public sealed record GetWorkoutsQuery(Guid UserId) : IRequest<IReadOnlyList<WorkoutCardDto>>;

//future additions: filter and sorting