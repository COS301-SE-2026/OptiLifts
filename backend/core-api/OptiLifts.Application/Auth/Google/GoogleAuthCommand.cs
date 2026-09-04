using MediatR;
using OptiLifts.Application.Auth.Register;

namespace OptiLifts.Application.Auth.Google;

public sealed record GoogleAuthCommand(string IdToken) : IRequest<AuthResponseDto>;
