namespace OptiLifts.Application.Auth.Register;

//returned to the client after auth operations
public sealed record AuthUserDto(Guid Id, string DisplayName, string Email, DateTime CreatedAt, bool Metric, bool lightTheme);

public sealed record AuthResponseDto(string AccessToken, string RefreshToken, AuthUserDto User);
