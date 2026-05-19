using MediatR;

namespace OptiLifts.Application.Auth.Register;

//request model for registering a new user
public sealed record RegisterUserCommand(string DisplayName, string Email, string Password) : IRequest<AuthResponseDto>;
