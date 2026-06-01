using MediatR;
using OptiLifts.Application.Auth.Register;

namespace OptiLifts.Application.Auth.Login;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<AuthResponseDto>;
