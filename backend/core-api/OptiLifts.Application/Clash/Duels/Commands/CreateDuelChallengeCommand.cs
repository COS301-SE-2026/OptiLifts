using MediatR;

namespace OptiLifts.Application.Clash.Duels.Commands;

public sealed record CreateDuelChallengeCommand(
    Guid ChallengerUserId,
    Guid RivalUserId,
    string ExerciseName,
    Guid? ExerciseId,
    string TargetType,
    int DurationDays
) : IRequest<CreateDuelChallengeResult>;

public sealed record CreateDuelChallengeResult(bool Success, string Message, Guid? DuelId = null);
