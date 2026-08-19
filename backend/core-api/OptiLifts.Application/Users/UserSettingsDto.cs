namespace OptiLifts.Application.Users;

public record UserSettingsDto
{
    public ProfileDto Profile { get; init; } = null!;
    public PreferencesDto Preferences { get; init; } = null!;
    public SecurityDto Security { get; init; } = null!;
}

public record ProfileDto(
    string DisplayName,
    string? Bio,
    string? Sex,
    DateTime? DateOfBirth,
    double? Weight,
    double? Height,
    string? ProfilePictureUrl
);

public record PreferencesDto(
    string Theme,
    string Units
);

public record SecurityDto(
    bool HasPassword
);