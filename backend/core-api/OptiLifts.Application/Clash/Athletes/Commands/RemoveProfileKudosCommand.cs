using MediatR;

namespace OptiLifts.Application.Clash.Athletes.Commands;

public sealed record RemoveProfileKudosCommand(Guid SenderUserId, Guid TargetUserId) : IRequest<RemoveProfileKudosResult>;

public sealed record RemoveProfileKudosResult(bool Success, string Message);
