using MediatR;

namespace OptiLifts.Application.Vision;

public record AnalyzeVisionCommand(VisionAnalyzeRequest Request) : IRequest<string>;
