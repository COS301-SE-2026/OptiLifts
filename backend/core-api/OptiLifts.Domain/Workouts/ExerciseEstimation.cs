namespace OptiLifts.Domain.Workouts;

public class ExerciseEstimation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExerciseId { get; set; }
    public Guid UserId { get; set; }
    public float? Weight { get; set; }
    public int Reps { get; set; }
    public ExerciseType ExerciseType { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public bool Deload { get; set; }
}
