using System.Text.Json.Serialization;
using OptiLifts.Domain.Training;

namespace OptiLifts.Application.Training.GetPlateauPage;

public sealed record WorkoutRefDto(Guid WorkoutId, string WorkoutName);

public sealed record ExerciseDiagnosisDto(
    Guid ExerciseId,
    string ExerciseName,
    string MuscleGroup,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TrendStatus Status,
    float SlopePctPerWeek,
    string? Recommendation,
    bool CanSwapExercise,
    DateTime ComputedAt,
    IReadOnlyList<WorkoutRefDto> Workouts);

