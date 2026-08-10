namespace OptiLifts.Application.Users;

public record UserSettingsDto
{
    public ProfileDto Profile { get; init; } = null!;
    public PreferencesDto Preferences { get; init; } = null!;

    public List<UserRepRangeDto> RepRanges { get; init; } = new();
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

public record UserRepRangeDto(
    Guid Id,
    string ExerciseType,
    int LowerLimit,
    int UpperLimit
);