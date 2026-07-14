namespace OptiLifts.Application.Profile;

public sealed record ProfileCalendarEntryDto(
    Guid WorkoutId,
    Guid LogId,
    string Date);

public sealed record ProfileCalendarDto(
    IReadOnlyList<ProfileCalendarEntryDto> Entries);