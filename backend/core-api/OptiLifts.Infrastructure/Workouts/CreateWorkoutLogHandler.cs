using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.CreateSession;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class CreateWorkoutLogHandler : IRequestHandler<CreateWorkoutLogCom, CreateWorkoutLogRes?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public CreateWorkoutLogHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateWorkoutLogRes?> Handle(CreateWorkoutLogCom request, CancellationToken cancellationToken)
    {
        var existingWorkout = await _dbContext.Workouts.AnyAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId && !w.IsDeleted, cancellationToken);

        if (!existingWorkout)
        {
            return null;
        }

        var existing = await _dbContext.WorkoutLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == request.LogId, cancellationToken);

        if (existing is not null)
        {
            return new CreateWorkoutLogRes(existing.Id, existing.EntryId ?? Guid.Empty, AlreadyExisted: true);
        }

        Guid entryId;

        if (request.EntryId is Guid providedEntryId)
        {
            var valid = await _dbContext.ScheduledEntries
                .AnyAsync(e => e.Id == providedEntryId && e.UserId == request.UserId && e.WorkoutId == request.WorkoutId, cancellationToken);

            if (!valid)
            {
                return null;
            }

            entryId = providedEntryId;
        }
        else
        {
            var entry = new ScheduledEntry
            {
                WorkoutId = request.WorkoutId,
                UserId = request.UserId,
                Scheduled = request.StartedAt,
                Status = ScheduleStatus.AdHoc
            };

            _dbContext.ScheduledEntries.Add(entry);
            entryId = entry.Id;
        }

        var log = new WorkoutLog
        {
            Id = request.LogId,
            EntryId = entryId,
            Notes = request.Notes,
            AiModified = false,
            StartedAt = request.StartedAt,
            CompletedAt = request.CompletedAt,
        };
        _dbContext.WorkoutLogs.Add(log);

        foreach (var exercise in request.Exercises)
        {
            _dbContext.WorkoutLogExercises.Add(new WorkoutLogExercise
            {
                LogId = log.Id,
                ExerciseId = exercise.ExerciseId,
                WorkoutExerciseId = exercise.WorkoutExerciseId,
                OrderIndex = exercise.OrderIndex,
                GroupNumber = exercise.GroupNumber
            });

            foreach (var set in exercise.Sets)
            {
                _dbContext.WorkoutLogSets.Add(new WorkoutSetLog
                {
                    LogId = log.Id,
                    ExerciseId = exercise.ExerciseId,
                    WorkoutExerciseId = exercise.WorkoutExerciseId,
                    SetId = set.SetId,
                    Type = ParseSetType(set.Type),
                    Reps = set.Reps,
                    Weight = set.Weight,
                    GroupNumber = set.GroupNumber,
                    Rpe = set.Rpe,
                    Duration = set.Duration,
                    Distance = set.Distance,
                    RestTime = set.RestTime,
                    OrderIndex = set.OrderIndex,
                    LoggedAt = request.CompletedAt,
                    AiSuggested = false
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new CreateWorkoutLogRes(log.Id, entryId, AlreadyExisted: false);
    }

    private static SetType ParseSetType(string value) => Enum.TryParse<SetType>(value, ignoreCase: true, out var parsed) ? parsed : SetType.Normal;
}
