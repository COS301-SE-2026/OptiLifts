using MediatR;
namespace OptiLifts.Application.Users;
public sealed record UpdateProfileDetailsCommand(
    Guid UserId,
    string DisplayName,
    string? Bio,
    string? Sex,
    string? DateOfBirth,
    double? Weight,
    double? Height
) : IRequest;