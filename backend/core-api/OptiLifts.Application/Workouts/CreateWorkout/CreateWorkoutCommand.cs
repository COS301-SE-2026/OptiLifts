using MediatR;

namespace OptiLifts.Application.Workouts.CreateWorkout;

//the request model (ie data needed to run command)
public sealed record CreateWorkoutSetDto(
    Guid ExerciseId,
    string Type,
    int Reps,
    float Weight,
    int OrderIndex,
    int RestTime
);

public sealed record CreateWorkoutCommand(
    Guid FolderId,
    string Name,
    int? DayIndex,
    Guid CreatedBy,
    IReadOnlyList<CreateWorkoutSetDto> Sets
) : IRequest<CreateWorkoutResult>;
