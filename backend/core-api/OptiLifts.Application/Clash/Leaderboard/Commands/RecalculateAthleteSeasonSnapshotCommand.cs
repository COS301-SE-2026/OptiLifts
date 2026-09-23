using MediatR;

namespace OptiLifts.Application.Clash.Leaderboard.Commands;

public sealed record RecalculateAthleteSeasonSnapshotCommand(Guid UserId) : IRequest;
