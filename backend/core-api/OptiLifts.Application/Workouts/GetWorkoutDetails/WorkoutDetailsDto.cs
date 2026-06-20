namespace OptiLifts.Application.Workouts.GetWorkoutDetails;

public sealed record WorkoutDetailsSetDto(
    Guid Id,
    string Type,
    float? Kg,
    int? Reps,
    int OrderIndex
);
public sealed record WorkoutDetailsExerciseDto(
    Guid Id,
    Guid ExerciseCatalogId,
    string Name,
    string Muscle,
    string? ImageUrl,
    IReadOnlyList<WorkoutDetailsSetDto> Sets,
    int OrderIndex
);
public sealed record WorkoutDetailsDto(
    Guid Id,
    string Name,
    Guid? FolderId,
    IReadOnlyList<WorkoutDetailsExerciseDto> Exercises
);