using MediatR;

namespace OptiLifts.Application.Training.GetPlateauPage;

public sealed record GetPlateauPageQuery(Guid UserId) : IRequest<IReadOnlyList<ExerciseDiagnosisDto>>;
