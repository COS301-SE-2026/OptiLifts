using MediatR;

namespace OptiLifts.Application.Workouts.CreateSession;

public sealed record CreateWorkoutLogCom(
    Guid UserId,
    Guid WorkoutId,
    Guid LogId,
    Guid? EntryId,
    string? Notes,
    DateTime StartedAt,
    DateTime CompletedAt,
    IReadOnlyList<CreateWorkoutLogExerciseReq> Exercises
) : IRequest<CreateWorkoutLogRes?>;
