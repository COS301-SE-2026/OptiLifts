using OptiLifts.Domain.Training;

namespace OptiLifts.Application.Training.GetPlateauPage;

public sealed record WorkoutRefDto(Guid WorkoutId, string WorkoutName);

public sealed record ExerciseDiagnosisDto(
    Guid ExerciseId,
    string ExerciseName,
    TrendStatus Status,
    float SlopePctPerWeek,
    string? Recommendation,
    bool CanSwapExercise,
    DateTime ComputedAt,
    IReadOnlyList<WorkoutRefDto> Workouts);
