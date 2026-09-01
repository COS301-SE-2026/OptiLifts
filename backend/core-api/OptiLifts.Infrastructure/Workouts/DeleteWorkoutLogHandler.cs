using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.DeleteWorkoutLog;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class DeleteWorkoutLogHandler : IRequestHandler<DeleteWorkoutLogCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IPlateauDetectionService _plateauDetectionService;

    public DeleteWorkoutLogHandler(OptiLiftsDbContext dbContext, IPlateauDetectionService plateauDetectionService)
    {
        _dbContext = dbContext;
        _plateauDetectionService = plateauDetectionService;
    }

    public async Task<bool> Handle(DeleteWorkoutLogCommand request, CancellationToken cancellationToken)
    {
        var log = await (
            from workoutLog in _dbContext.WorkoutLogs
            join entry in _dbContext.ScheduledEntries on workoutLog.EntryId equals entry.Id
            where workoutLog.Id == request.LogId
                && entry.WorkoutId == request.WorkoutId
                && entry.UserId == request.UserId
            select workoutLog
        ).FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            return false;
        }

        var affectedExerciseIds = await _dbContext.WorkoutLogSets
            .Where(s => s.LogId == log.Id)
            .Select(s => s.ExerciseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _dbContext.WorkoutLogs.Remove(log);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var exerciseId in affectedExerciseIds)
        {
            await _plateauDetectionService.DetectAsync(request.UserId, exerciseId, cancellationToken);
        }

        return true;
    }
}