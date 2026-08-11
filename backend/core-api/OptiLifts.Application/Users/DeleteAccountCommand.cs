using MediatR;

namespace OptiLifts.Application.Users;

public sealed record DeleteAccountCommand(Guid UserId) : IRequest;