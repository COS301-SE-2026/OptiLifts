using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Workouts.UpdateWorkout;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class UpdateWorkoutHandler : IRequestHandler<UpdateWorkoutCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;

    public UpdateWorkoutHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateWorkoutCommand request, CancellationToken cancellationToken)
    {
        var workout = await _dbContext.Workouts
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.CreatedBy == request.UserId, cancellationToken);

        if (workout == null)
        {
            return false;
        }
        workout.Name = request.Name;
        workout.FolderId = request.FolderId;

        var existing = await _dbContext.WorkoutExercises
            .Where(we => we.WorkoutId == workout.Id)
            .ToListAsync(cancellationToken);
        if (existing.Any())
        {
            _dbContext.WorkoutExercises.RemoveRange(existing);
        }

        var existingGroups = await _dbContext.ExerciseGroups
            .Where(eg => eg.WorkoutId == workout.Id)
            .ToListAsync(cancellationToken);
        if (existingGroups.Any())
        {
            _dbContext.ExerciseGroups.RemoveRange(existingGroups);
        }

        var groupKeyToId = new Dictionary<string, Guid>();
        foreach(var group in request.Groups)
        {
            var exerciseGroup = new ExerciseGroup
            {
                WorkoutId = workout.Id,
                Type = ParseGroupType(group.Type),
                RestTime = group.RestTime
            };
            _dbContext.ExerciseGroups.Add(exerciseGroup);
            groupKeyToId[group.GroupKey] = exerciseGroup.Id;
        }

        foreach (var exercise in request.Exercises)
        {
            Guid? groupId = exercise.GroupKey is not null && groupKeyToId.TryGetValue(exercise.GroupKey, out var resolvedId)
            ? resolvedId : null;

            var workoutExercise = new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = exercise.ExerciseId,
                OrderIndex = exercise.OrderIndex,
                GroupId = groupId
            };
            _dbContext.WorkoutExercises.Add(workoutExercise);


            var sets = exercise.Sets.Select(s => new WorkoutSet
            {
                WorkoutExerciseId = workoutExercise.Id,
                Type = MapFrontendToSetType(s.Type),
                Reps = s.Reps,
                Weight = s.Weight,
                Duration = s.Duration,
                Distance = s.Distance,
                OrderIndex = s.OrderIndex,
                RestTime = s.RestTime
            });
            _dbContext.Sets.AddRange(sets);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
    private static SetType MapFrontendToSetType(string type) => type.ToUpperInvariant() switch
    {
        "W" => SetType.Warmup,
        "D" => SetType.DropSet,
        "I" => SetType.Normal,
        _ => Enum.TryParse<SetType>(type, true, out var parsed) ? parsed : SetType.Normal
    };
    private static ExerciseGroupType ParseGroupType(string value) 
    => Enum.TryParse<ExerciseGroupType>(value, ignoreCase: true, out var type) ? type : ExerciseGroupType.Circuit;
}