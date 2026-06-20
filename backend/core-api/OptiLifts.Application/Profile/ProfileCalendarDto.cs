namespace OptiLifts.Application.Profile;

public sealed record ProfileCalendarDto(
    IReadOnlyList<string> HighlightedDates);