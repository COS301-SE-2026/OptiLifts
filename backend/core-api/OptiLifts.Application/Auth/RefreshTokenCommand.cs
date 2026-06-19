using MediatR;
using OptiLifts.Application.Auth.Register;

namespace OptiLifts.Application.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;