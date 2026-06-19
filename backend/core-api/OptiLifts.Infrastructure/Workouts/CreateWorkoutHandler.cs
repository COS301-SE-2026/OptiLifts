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
            DayIndex = request.DayIndex,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Workouts.Add(workout);

        foreach (var exercise in request.Exercises)
        {
            var workoutExercise = new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = exercise.ExerciseId,
                OrderIndex = exercise.OrderIndex
            };
            _dbContext.WorkoutExercises.Add(workoutExercise);


            var sets = exercise.Sets.Select(s => new WorkoutSet
            {
                WorkoutExerciseId = workoutExercise.Id,
                Type = Enum.Parse<SetType>(s.Type, ignoreCase: true),
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
            workout.DayIndex,
            workout.CreatedAt
        );
    }
}
