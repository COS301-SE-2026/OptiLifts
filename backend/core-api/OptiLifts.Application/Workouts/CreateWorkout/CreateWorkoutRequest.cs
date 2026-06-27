namespace OptiLifts.Application.Workouts.CreateWorkout;

//request body shape from client
public sealed record CreateWorkoutSetRequest(
    string Type,
    int? Reps,
    float? Weight,
    int? Duration,
    float? Distance,
    int OrderIndex,
    int RestTime
);

public sealed record CreateWorkoutExerciseRequest(
    Guid ExerciseId,
    int OrderIndex,
    IReadOnlyList<CreateWorkoutSetRequest> Sets
);

public sealed record CreateWorkoutRequest(
    Guid? FolderId,
    string Name,
    IReadOnlyList<CreateWorkoutExerciseRequest> Exercises
);