namespace OptiLifts.Application.Workouts.DuplicateWorkout;

public sealed record DuplicateWorkoutResult(
    Guid WorkoutId,
    string Name,
    Guid? FolderId,
    DateTime CreatedAt
);