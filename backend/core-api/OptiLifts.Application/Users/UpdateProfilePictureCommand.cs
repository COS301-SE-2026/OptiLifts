using MediatR;

namespace OptiLifts.Application.Users;

public sealed record UploadProfilePictureCommand(
    Guid UserId,
    Stream FileStream,
    string FileName,
    string ContentType
) : IRequest<string>;