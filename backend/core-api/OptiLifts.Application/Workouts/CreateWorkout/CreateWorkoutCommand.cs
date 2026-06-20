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
    IReadOnlyList<CreateWorkoutSetDto> Sets
);

public sealed record CreateWorkoutCommand(
    Guid? FolderId,
    string Name,
    int? DayIndex,
    Guid CreatedBy,
    IReadOnlyList<CreateWorkoutExerciseDto> Exercises
) : IRequest<CreateWorkoutResult>;
