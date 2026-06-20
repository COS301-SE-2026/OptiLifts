using MediatR;

namespace OptiLifts.Application.Workouts.UpdateWorkout;

public sealed record UpdateWorkoutSetDto(
    string Type,
    int? Reps,
    float? Weight,
    int? Duration,
    float? Distance,
    int OrderIndex,
    int RestTime
);

public sealed record UpdateWorkoutExerciseDto(
    Guid ExerciseId,
    int OrderIndex,
    IReadOnlyList<UpdateWorkoutSetDto> Sets
);

public sealed record UpdateWorkoutCommand(
    Guid WorkoutId,
    Guid UserId,
    Guid? FolderId,
    string Name,
    IReadOnlyList<UpdateWorkoutExerciseDto> Exercises
) : IRequest<bool>;

public sealed record UpdateWorkoutRequest(
    Guid? FolderId,
    string Name,
    IReadOnlyList<UpdateWorkoutExerciseDto> Exercises 
);