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
    string? GroupKey,
    IReadOnlyList<CreateWorkoutSetRequest> Sets
);

public sealed record CreateWorkoutGroupRequest(
    string GroupKey,
    string Type,
    int Rounds,
    int RestTime
);

public sealed record CreateWorkoutRequest(
    Guid? FolderId,
    string Name,
    IReadOnlyList<CreateWorkoutExerciseRequest> Exercises,
    IReadOnlyList<CreateWorkoutGroupRequest> Groups
);