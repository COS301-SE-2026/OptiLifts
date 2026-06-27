using MediatR;
namespace OptiLifts.Application.Users;

public sealed record UpdateUserPreferencesCommand(Guid UserId, string Theme, string Units) : IRequest;