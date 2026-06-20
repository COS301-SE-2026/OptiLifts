namespace OptiLifts.Application.Profile;

public sealed record ProfileOverviewDto(
    ProfileUserDto Profile,
    IReadOnlyList<ProfileStatDto> Stats,
    IReadOnlyList<ProfileBadgeDto> Badges,
    IReadOnlyList<ProfileWorkoutDto> RecentWorkouts,
    string ChartTitle,
    IReadOnlyList<ProfileChartDatumDto> ChartData);

public sealed record ProfileUserDto(
    string Name,
    string Email,
    string? Bio,
    string? ProfileImageUrl = null);

public sealed record ProfileStatDto(
    string Label,
    string Value);

public sealed record ProfileBadgeDto(
    string Name,
    string Description,
    string Category,
    string? IconUrl,
    DateTime EarnedAt);

public sealed record ProfileWorkoutDto(
    string Name,
    IReadOnlyList<string> Exercises,
    string Prs,
    string Duration,
    string Volume,
    string Sets);

public sealed record ProfileChartDatumDto(
    string Label,
    double Value);