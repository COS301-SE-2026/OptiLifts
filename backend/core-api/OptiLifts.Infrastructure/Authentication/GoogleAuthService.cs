using Google.Apis.Auth;
using OptiLifts.Application.Auth.Abstractions;

namespace OptiLifts.Infrastructure.Authentication;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly string _clientId;

    public GoogleAuthService(string clientId)
    {
        _clientId = clientId;
    }

    public async Task<GoogleUserInfoDto> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException("Google ID token cannot be empty.", nameof(idToken));
        }

        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = string.IsNullOrWhiteSpace(_clientId) ? null : new[] { _clientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

        if (payload == null || string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new InvalidJwtException("Invalid token payload: missing subject or email.");
        }

        return new GoogleUserInfoDto(
            payload.Subject,
            payload.Email,
            payload.Name,
            payload.Picture
        );
    }
}
