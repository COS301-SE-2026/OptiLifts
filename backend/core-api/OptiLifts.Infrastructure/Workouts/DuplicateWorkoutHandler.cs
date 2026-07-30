using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.DuplicateWorkout;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class DuplicateWorkoutHandler : IRequestHandler<DuplicateWorkoutCommand, DuplicateWorkoutResult?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public DuplicateWorkoutHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DuplicateWorkoutResult?> Handle(DuplicateWorkoutCommand request, CancellationToken cancellationToken)
    {
        var sourceWork = await _dbContext.Workouts
            .FirstOrDefaultAsync(w => w.Id == request.SourceWorkoutId && w.CreatedBy == request.UserId && !w.IsDeleted, cancellationToken);

        if (sourceWork == null)
        {
            return null;
        }

        var duplicateWorkout = new Workout
        {
            Id = Guid.NewGuid(),
            FolderId = sourceWork.FolderId,
            Name = sourceWork.Name,
            CreatedBy = request.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Workouts.Add(duplicateWorkout);

        var sourceGroups = await _dbContext.ExerciseGroups
            .AsNoTracking()
            .Where(eg => eg.WorkoutId == sourceWork.Id)
            .ToListAsync(cancellationToken);
        var groupIdMap = new Dictionary<Guid, Guid>();
        foreach (var sourcegroup in sourceGroups)
        {
            var dupeGroup = new ExerciseGroup
            {
                Id = Guid.NewGuid(),
                WorkoutId = duplicateWorkout.Id,
                Type = sourcegroup.Type,
                RestTime = sourcegroup.RestTime
            };
            _dbContext.ExerciseGroups.Add(dupeGroup);
            groupIdMap[sourcegroup.Id] = dupeGroup.Id;
        }

        var sourceExercises = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(we => we.WorkoutId == sourceWork.Id)
            .ToListAsync(cancellationToken);

        if (sourceExercises.Count > 0)
        {
            var sourceIds = sourceExercises.Select(we => we.Id).ToList();

            var sourceSets = await _dbContext.Sets
                .AsNoTracking()
                .Where(s => sourceIds.Contains(s.WorkoutExerciseId))
                .ToListAsync(cancellationToken);

            foreach (var sourceExercise in sourceExercises)
            {
                var dupeExercise = new WorkoutExercise
                {
                    Id = Guid.NewGuid(),
                    WorkoutId = duplicateWorkout.Id,
                    ExerciseId = sourceExercise.ExerciseId,
                    OrderIndex = sourceExercise.OrderIndex,
                    GroupId = sourceExercise.GroupId.HasValue && groupIdMap.TryGetValue(sourceExercise.GroupId.Value, out var newGroupId) ? newGroupId : null
                };
                _dbContext.WorkoutExercises.Add(dupeExercise);

                var assocSets = sourceSets.Where(s => s.WorkoutExerciseId == sourceExercise.Id);
                foreach (var sourceSet in assocSets)
                {
                    var dupeSet = new WorkoutSet
                    {
                        Id = Guid.NewGuid(),
                        WorkoutExerciseId = dupeExercise.Id,
                        Type = sourceSet.Type,
                        Reps = sourceSet.Reps,
                        Weight = sourceSet.Weight,
                        Duration = sourceSet.Duration,
                        Distance = sourceSet.Distance,
                        OrderIndex = sourceSet.OrderIndex,
                        RestTime = sourceSet.RestTime
                    };
                    _dbContext.Sets.Add(dupeSet);
                }
            }


        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new DuplicateWorkoutResult(
            duplicateWorkout.Id,
            duplicateWorkout.Name,
            duplicateWorkout.FolderId,
            duplicateWorkout.CreatedAt
        );
    }
}