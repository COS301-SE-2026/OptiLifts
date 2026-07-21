namespace OptiLifts.Application.Workouts.GetWorkoutLogDetail;

public sealed record WorkoutLogSetDto(
    Guid Id,
    Guid? SetId,
    string Type,
    int Reps,
    float Weight,
    int OrderIndex,
    int? Duration,
    float? Distance,
    int RestTime,
    int GroupNumber,
    float Rpe);

public sealed record WorkoutLogExerciseDetailDto(
    Guid Id,
    Guid ExerciseId,
    string Name,
    string PrimaryMuscle,
    string ExerciseType,
    int OrderIndex,
    WorkoutLogSetDto[] Sets);

public sealed record WorkoutLogDetailDto(
    Guid WorkoutId,
    Guid LogId,
    string Name,
    Guid? FolderId,
    int? DayIndex,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? Duration,
    string[] PrimaryMuscleGroups,
    string[] ExercisePreview,
    WorkoutLogExerciseDetailDto[] Exercises);