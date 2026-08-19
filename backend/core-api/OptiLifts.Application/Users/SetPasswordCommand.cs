using MediatR;

namespace OptiLifts.Application.Users;

public sealed record SetPasswordCommand(Guid UserId, string NewPassword) : IRequest;
