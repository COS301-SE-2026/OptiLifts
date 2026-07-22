using MediatR;

namespace OptiLifts.Application.Exercises.GetExerciseImages;

public sealed record GetExerciseImagesQuery(List<Guid> ExerciseIds) : IRequest<Dictionary<string, string>>;