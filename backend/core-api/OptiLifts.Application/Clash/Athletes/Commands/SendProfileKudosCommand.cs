using MediatR;

namespace OptiLifts.Application.Clash.Athletes.Commands;

public sealed record SendProfileKudosCommand(Guid SenderUserId, Guid TargetUserId) : IRequest<SendProfileKudosResult>;

public sealed record SendProfileKudosResult(bool Success, string Message);
