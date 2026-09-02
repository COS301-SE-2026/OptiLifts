using MediatR;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Application.Users;

public sealed record UpdateUserRepRangeCommand(
    Guid UserId,
    Guid RepRangeId,
    UserRepRangeExerciseType ExerciseType,
    int LowerLimit,
    int UpperLimit
) : IRequest;
