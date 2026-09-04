namespace OptiLifts.Application.Workouts.GetWorkoutDetail;

public sealed record WorkoutSetDto(
    Guid Id,
    string Type,
    int? Reps,
    float? Weight,
    int? Duration,
    float? Distance,
    int OrderIndex,
    int RestTime,
    float? PreviousWeight = null,
    int? PreviousReps = null
);

public sealed record ExerciseEstimationDto(float? Weight, int Reps);

public sealed record WorkoutExerciseDetailDto(
    Guid Id,
    Guid ExerciseId,
    string Name,
    string PrimaryMuscle,
    string[] SecondaryMuscles,
    string ExerciseType,
    int OrderIndex,
    WorkoutSetDto[] Sets,
    Guid? GroupId = null,
    string? GroupType = null,
    int? GroupRestTime = null,
    string? ImageUrl = null,
    float? BestWeight = null,
    float? BestSetVolume = null,
    ExerciseEstimationDto? Estimation = null,
    bool IsMachine = false
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