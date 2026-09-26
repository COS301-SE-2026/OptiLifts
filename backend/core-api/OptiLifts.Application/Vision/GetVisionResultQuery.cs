using MediatR;

namespace OptiLifts.Application.Vision;

public record GetVisionResultQuery(string JobId) : IRequest<VisionResultResponse?>;
