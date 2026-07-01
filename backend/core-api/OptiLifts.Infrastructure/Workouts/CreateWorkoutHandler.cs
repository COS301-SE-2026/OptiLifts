using MediatR;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Workouts;

public sealed class CreateWorkoutHandler : IRequestHandler<CreateWorkoutCommand, CreateWorkoutResult>
{
    private readonly OptiLiftsDbContext _dbContext;

    public CreateWorkoutHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateWorkoutResult> Handle(CreateWorkoutCommand request, CancellationToken cancellationToken)
    {
        var workout = new Workout
        {
            FolderId = request.FolderId,
            Name = request.Name,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Workouts.Add(workout);

        var groupKeyToId = new Dictionary<string, Guid>();
        foreach (var group in request.Groups)
        {
            var exerciseGroup = new ExerciseGroup
            {
                WorkoutId = workout.Id,
                Type = ParseGroupType(group.Type),
                Rounds = group.Rounds,
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
                Type = ParseSetType(s.Type),
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

        return new CreateWorkoutResult(
            workout.Id,
            workout.Name,
            workout.FolderId,
            workout.CreatedAt
        );
    }
    private static SetType ParseSetType(string value) => 
        Enum.TryParse<SetType>(value, ignoreCase: true, out var type) ? type : SetType.Normal;

    private static ExerciseGroupType ParseGroupType(string value) => 
        Enum.TryParse<ExerciseGroupType>(value, ignoreCase: true, out var type) ? type : ExerciseGroupType.Circuit;
}


