using MediatR;

namespace OptiLifts.Application.Auth.Logout;

public sealed record LogoutCommand(Guid UserId) : IRequest;