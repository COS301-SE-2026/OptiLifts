using MediatR;

namespace OptiLifts.Application.Workouts.GetWorkoutDetails;

public sealed record WorkoutSetDetailDto(
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
    Guid WorkoutExerciseId,
    Guid ExerciseId,
    string Name,
    string MuscleGroup,
    int OrderIndex,
    IReadOnlyList<WorkoutSetDetailDto> Sets
);

public sealed record WorkoutDetailDto(
    Guid Id,
    string Name,
    string[] PrimaryMuscleGroups,
    IReadOnlyList<WorkoutExerciseDetailDto> Exercises
);

public sealed record GetWorkoutDetailsQuery(Guid WorkoutId, Guid UserId) : IRequest<WorkoutDetailDto?>;
