using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.DeleteScheduledSession;
using OptiLifts.Infrastructure.Database;
namespace OptiLifts.Infrastructure.Scheduling;

public sealed class DeleteScheduledSessionHandler : IRequestHandler<DeleteScheduledSessionCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;
    public DeleteScheduledSessionHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<bool> Handle(DeleteScheduledSessionCommand request, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.ScheduledEntries.FirstOrDefaultAsync(e=> e.Id == request.SessionId && e.UserId == request.UserId, cancellationToken);
        if (entry == null)
        {
            return false; //does not exist or doesnt belong to user
        }

        _dbContext.ScheduledEntries.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}