using MediatR;

namespace OptiLifts.Application.Exercises.GetExerciseImages;

public sealed record GetExerciseImagesQuery(List<string> ExerciseNames) : IRequest<Dictionary<string, string>>;