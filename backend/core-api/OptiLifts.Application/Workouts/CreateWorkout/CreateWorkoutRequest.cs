namespace OptiLifts.Application.Workouts.CreateWorkout;

//request body shape from client
public sealed record CreateWorkoutSetRequest(
    Guid ExerciseId,
    string Type,
    int Reps,
    float Weight,
    int OrderIndex,
    int RestTime
);

public sealed record CreateWorkoutRequest(
    Guid FolderId,
    string Name,
    int? DayIndex,
    IReadOnlyList<CreateWorkoutSetRequest> Sets
);
