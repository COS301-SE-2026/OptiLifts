using MediatR;

namespace OptiLifts.Application.Vision;

public record ProcessWorkerResultCommand(VisionWorkerResult Result) : IRequest<bool>;
