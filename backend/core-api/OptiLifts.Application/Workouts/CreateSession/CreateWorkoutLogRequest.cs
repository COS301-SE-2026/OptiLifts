namespace OptiLifts.Application.Workouts.CreateSession;

public sealed record CreateWorkoutLogSetReq(
    Guid? SetId,
    string Type,
    int Reps,
    float Weight,
    int? Duration,
    float? Distance,
    int RestTime,
    float Rpe,
    int OrderIndex,
    int GroupNumber
);

public sealed record CreateWorkoutLogReq(
    Guid LogId,
    Guid? EntryId,
    string? Notes,
    DateTime StartedAt,
    DateTime CompletedAt,
    IReadOnlyList<CreateWorkoutLogExerciseReq> Exercises
);

public sealed record CreateWorkoutLogExerciseReq(
    Guid ExerciseId,
    Guid? WorkoutExerciseId,
    int OrderIndex,
    int GroupNumber,
    IReadOnlyList<CreateWorkoutLogSetReq> Sets
);
