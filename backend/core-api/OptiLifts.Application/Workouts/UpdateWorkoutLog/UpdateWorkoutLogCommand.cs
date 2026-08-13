using MediatR;

namespace OptiLifts.Application.Workouts.UpdateWorkoutLog;

public sealed record UpdateWorkoutLogSetDto(
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

public sealed record UpdateWorkoutLogExerciseDto(
    Guid ExerciseId,
    Guid? WorkoutExerciseId,
    int OrderIndex,
    int GroupNumber,
    IReadOnlyList<UpdateWorkoutLogSetDto> Sets
);

public sealed record UpdateWorkoutLogCommand(
    Guid UserId,
    Guid WorkoutId,
    Guid LogId,
    string? Notes,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<UpdateWorkoutLogExerciseDto> Exercises
) : IRequest<bool>;
