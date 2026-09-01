using MediatR;
using OptiLifts.Application.Training.RecordAcuteFatigue;
using OptiLifts.Domain.Training;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Training;

public sealed class RecordAcuteFatigueHandler : IRequestHandler<RecordAcuteFatigueCommand>
{
    private readonly OptiLiftsDbContext _dbContext;

    public RecordAcuteFatigueHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RecordAcuteFatigueCommand request, CancellationToken cancellationToken)
    {
        _dbContext.TrainingEvents.Add(new TrainingEvent
        {
            UserId = request.UserId,
            Type = TrainingEventType.AcuteFatigueFlagged,
            Scope = request.MuscleGroup,
            Diagnosis = $"Acute fatigue flagged for {request.MuscleGroup}"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
