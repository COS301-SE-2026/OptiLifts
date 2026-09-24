using MediatR;

namespace OptiLifts.Application.Clash.Leaderboard.Commands;

public sealed record UserProfileUpdatedCommand(Guid UserId) : IRequest;
