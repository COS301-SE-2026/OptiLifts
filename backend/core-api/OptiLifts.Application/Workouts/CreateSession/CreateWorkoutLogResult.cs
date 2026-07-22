namespace OptiLifts.Application.Workouts.CreateSession;

public sealed record CreateWorkoutLogRes(
    Guid LogId,
    Guid EntryId,
    bool AlreadyExisted
);
