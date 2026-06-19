namespace OptiLifts.Domain.Workouts;

public class SecMuscle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MuscleId { get; set; }
    public Guid ExerciseId { get; set; }
}