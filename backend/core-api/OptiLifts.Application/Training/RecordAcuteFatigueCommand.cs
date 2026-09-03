using MediatR;

namespace OptiLifts.Application.Training.RecordAcuteFatigue;

public sealed record RecordAcuteFatigueCommand(Guid UserId, string MuscleGroup) : IRequest;
