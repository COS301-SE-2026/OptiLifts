using MediatR;
namespace OptiLifts.Application.Users;

public sealed record UpdatePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;