using MediatR;

namespace OptiLifts.Application.Clash.Leaderboard.Queries;

public sealed record GetDivisionalLeaderboardQuery(
    Guid RequestingUserId,
    string Gender,
    string BracketId,
    string Metric,
    string Timeframe = "monthly",
    int Page = 1,
    int PageSize = 10
) : IRequest<LeaderboardPageResult>;
