using MediatR;
using OptiLifts.Application.Exercises.CreateCustomExercise;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Exercises.CreateCustomExercise;

public class CreateCustomExerciseHandler : IRequestHandler<CreateCustomExerciseCommand, Guid>
{
    private readonly OptiLiftsDbContext _dbContext;

    public CreateCustomExerciseHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateCustomExerciseCommand request, CancellationToken cancellationToken)
    {
        var exercise = new Exercise
        {
            UserId = request.UserId,
            Name = request.Name,
            Mechanic = request.Mechanic,
            Equipment = request.Equipment,
            Category = request.Category,
            PrimaryMuscles = request.PrimaryMuscles,
            SecondaryMuscles = request.SecondaryMuscles
        };

        _dbContext.Exercises.Add(exercise);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return exercise.Id;
    }
}