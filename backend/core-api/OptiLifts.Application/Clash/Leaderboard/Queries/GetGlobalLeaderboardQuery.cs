using MediatR;

namespace OptiLifts.Application.Clash.Leaderboard.Queries;

public sealed record GetGlobalLeaderboardQuery(
    Guid RequestingUserId,
    string Metric,
    string Timeframe = "monthly",
    int Page = 1,
    int PageSize = 10
) : IRequest<LeaderboardPageResult>;

public sealed record LeaderboardPageResult(
    IReadOnlyList<LeaderboardEntryDto> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    LeaderboardEntryDto? CurrentUserEntry,
    bool IsUserOptedIn = false
);
