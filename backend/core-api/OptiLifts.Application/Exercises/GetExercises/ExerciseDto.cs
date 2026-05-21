namespace OptiLifts.Application.Exercises.GetExercises;

public record ExerciseDto(
    Guid Id,
    string Name,
    string? Mechanic,
    string? Equipment,
    string Category,
    List<string> PrimaryMuscles,
    List<string> SecondaryMuscles,
    bool IsCustom
);
