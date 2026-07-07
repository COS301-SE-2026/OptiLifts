using MediatR;

namespace OptiLifts.Application.Exercises.GetExerciseImages;

public sealed record GetExerciseImagesQuery() : IRequest<Dictionary<string, string>>;