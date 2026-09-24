using MediatR;

namespace OptiLifts.Application.Clash.Leaderboard.Commands;

public sealed record ToggleLeaderboardOptInCommand(Guid UserId, bool OptIn) : IRequest<ToggleLeaderboardOptInResult>;

public sealed record ToggleLeaderboardOptInResult(bool Success, bool IsOptedIn);
