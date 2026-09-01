using System.Text.Json.Serialization;
using OptiLifts.Domain.Training;

namespace OptiLifts.Application.Training.GetPlateauPage;

public sealed record ExerciseDiagnosisDto(
    Guid ExerciseId,
    string ExerciseName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TrendStatus Status,
    float SlopePctPerWeek,
    string? Recommendation,
    DateTime ComputedAt);
