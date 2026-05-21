namespace OptiLifts.Application.Workouts.CreateWorkout;

//return shape sent to client
public sealed record CreateWorkoutResult(
    Guid WorkoutId,
    string Name,
    Guid? FolderId,
    int? DayIndex,
    DateTime CreatedAt
);
