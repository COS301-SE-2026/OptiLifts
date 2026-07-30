using MediatR;

namespace OptiLifts.Application.Workouts.DuplicateWorkout;

public sealed record DuplicateWorkoutCommand(Guid SourceWorkoutId, Guid UserId) : IRequest<DuplicateWorkoutResult?>;
