namespace OptiLifts.Application.Profile;

public sealed record ProfileOverviewDto(
    ProfileUserDto Profile,
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
    string Description, //for in the future, if we add the ability to click on a badge to get more information
    string Category,
    DateTime EarnedAt);

public sealed record ProfileWorkoutDto(
    Guid WorkoutId,
    Guid? LogId,
    string Name,
    IReadOnlyList<string> Exercises,
    string Prs,
    string Duration,
    string Volume,
    string Sets);

public sealed record ProfileChartDatumDto(
    string Label,
    double Value);