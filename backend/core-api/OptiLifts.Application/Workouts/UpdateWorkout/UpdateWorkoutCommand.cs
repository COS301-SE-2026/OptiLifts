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
    string? GroupKey,
    IReadOnlyList<UpdateWorkoutSetDto> Sets
);

public sealed record UpdateWorkoutGroupDto(
    string GroupKey,
    string Type,
    int RestTime
);

public sealed record UpdateWorkoutCommand(
    Guid WorkoutId,
    Guid UserId,
    Guid? FolderId,
    string Name,
    IReadOnlyList<UpdateWorkoutExerciseDto> Exercises,
    IReadOnlyList<UpdateWorkoutGroupDto> Groups
) : IRequest<bool>;

public sealed record UpdateWorkoutRequest(
    Guid? FolderId,
    string Name,
    IReadOnlyList<UpdateWorkoutExerciseDto> Exercises,
    IReadOnlyList<UpdateWorkoutGroupDto> Groups
);