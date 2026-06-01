namespace OptiLifts.Application.Auth.Register;

//returned to the client after auth operations
public sealed record AuthUserDto(Guid Id, string DisplayName, string Email, DateTime CreatedAt);

public sealed record AuthResponseDto(string Token, AuthUserDto User);
