using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.CreateScheduledSession;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class CreateScheduledSessionHandler : IRequestHandler<CreateScheduledSessionCommand, CreateScheduledSessionResult?>
{
    private readonly OptiLiftsDbContext _dbContext;
    public CreateScheduledSessionHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<CreateScheduledSessionResult?> Handle(CreateScheduledSessionCommand request, CancellationToken cancellationToken)
    {
        var workoutExist = await _dbContext.Workouts.AnyAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId, cancellationToken);
        if (!workoutExist)
        {
            return null;
        }

        //new entry to insert
        var entry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            WorkoutId = request.WorkoutId,
            Scheduled = request.ScheduledAt,
            Status = request.Status ?? ScheduleStatus.Scheduled
        };
        _dbContext.ScheduledEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateScheduledSessionResult(
            entry.Id,
            entry.WorkoutId,
            entry.Scheduled,
            entry.Status
        );
    }
}