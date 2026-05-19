namespace OptiLifts.Application.Workouts.GetWorkouts;

//return shape sent to client
public sealed record WorkoutCardDto(
    Guid Id,
    string Name,
    string[] PrimaryMuscleGroups,
    int ExerciseCount,
    string[] ExercisePreview,
    DateTime CreatedAt
);
