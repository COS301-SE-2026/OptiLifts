namespace OptiLifts.Application.Workouts.GetWorkoutDetail;

public sealed record WorkoutSetDto(
    Guid Id,
    string Type,
    int? Reps,
    float? Weight,
    int? Duration,
    float? Distance,
    int OrderIndex,
    int RestTime
);

public sealed record WorkoutExerciseDetailDto(
    Guid Id,
    Guid ExerciseId,
    string Name,
    string PrimaryMuscle,
    string ExerciseType,
    int OrderIndex,
    WorkoutSetDto[] Sets,
    Guid? GroupId = null,
    string? GroupType = null,
    int? GroupRestTime = null,
    string? ImageUrl = null
);

public sealed record WorkoutDetailDto(
    Guid Id,
    string Name,
    Guid? FolderId,
    int? DayIndex,
    DateTime CreatedAt,
    string[] PrimaryMuscleGroups,
    string[] ExercisePreview,
    WorkoutExerciseDetailDto[] Exercises
);