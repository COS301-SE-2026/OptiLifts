using MediatR;
using OptiLifts.Domain.ProgressiveOverload;

namespace OptiLifts.Application.ProgressiveOverload;

public record GenerateOverloadCommand(
    Guid UserId, 
    Guid ExerciseId
): IRequest<List<PODataPoint>>;