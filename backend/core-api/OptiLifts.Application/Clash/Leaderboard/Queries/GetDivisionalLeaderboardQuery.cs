using MediatR;

namespace OptiLifts.Application.Clash.Leaderboard.Queries;

public sealed record GetDivisionalLeaderboardQuery(
    Guid RequestingUserId,
    string Gender,
    string BracketId,
    string Metric,
    int Page,
    int PageSize
) : IRequest<LeaderboardPageResult>;
