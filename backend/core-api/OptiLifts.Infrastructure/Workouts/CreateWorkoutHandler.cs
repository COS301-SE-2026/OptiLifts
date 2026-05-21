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

        var sets = request.Sets.Select(s => new WorkoutSet
        {
            WorkoutId = workout.Id,
            ExerciseId = s.ExerciseId,
            Type = Enum.Parse<SetType>(s.Type, ignoreCase: true),
            Reps = s.Reps,
            Weight = s.Weight,
            OrderIndex = s.OrderIndex,
            RestTime = s.RestTime
        });

        _dbContext.Sets.AddRange(sets);

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
