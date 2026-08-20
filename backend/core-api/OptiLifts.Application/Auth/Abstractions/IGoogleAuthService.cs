namespace OptiLifts.Application.Auth.Abstractions;

public sealed record GoogleUserInfoDto(
    string GoogleId,
    string Email,
    string? Name,
    string? PictureUrl
);

public interface IGoogleAuthService
{
    Task<GoogleUserInfoDto> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
