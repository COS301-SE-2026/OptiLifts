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
}