using MediatR;
namespace OptiLifts.Application.Users;
public sealed record DeleteProfilePictureCommand(Guid UserId) : IRequest;