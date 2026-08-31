namespace OptiLifts.Application.Workouts.UpdateWorkoutLog;

public sealed record UpdateWorkoutLogSetReq(
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

public sealed record UpdateWorkoutLogReq(
    string? Notes,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<UpdateWorkoutLogExerciseReq> Exercises
);

public sealed record UpdateWorkoutLogExerciseReq(
    Guid ExerciseId,
    Guid? WorkoutExerciseId,
    int OrderIndex,
    int GroupNumber,
    IReadOnlyList<UpdateWorkoutLogSetReq> Sets
);
