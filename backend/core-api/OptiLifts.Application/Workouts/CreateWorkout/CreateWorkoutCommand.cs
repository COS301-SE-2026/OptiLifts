using MediatR;

namespace OptiLifts.Application.Workouts.CreateWorkout;

//the request model (ie data needed to run command)
public sealed record CreateWorkoutSetDto(
    string Type,
    int? Reps,
    float? Weight,
    int? Duration,
    float? Distance,
    int OrderIndex,
    int RestTime
);

public sealed record CreateWorkoutExerciseDto(
    Guid ExerciseId,
    int OrderIndex,
    string? GroupKey,
    IReadOnlyList<CreateWorkoutSetDto> Sets
);

public sealed record CreateWorkoutGroupDto(
    string GroupKey,
    string Type,
    int RestTime
);

public sealed record CreateWorkoutCommand(
    Guid? FolderId,
    string Name,
    Guid CreatedBy,
    IReadOnlyList<CreateWorkoutExerciseDto> Exercises,
    IReadOnlyList<CreateWorkoutGroupDto> Groups
) : IRequest<CreateWorkoutResult>;
