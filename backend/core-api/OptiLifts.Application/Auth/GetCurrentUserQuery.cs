using MediatR;
using OptiLifts.Application.Auth.Register;

namespace OptiLifts.Application.Auth.Me;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<AuthUserDto>;