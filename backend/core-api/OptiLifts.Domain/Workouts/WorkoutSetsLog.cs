namespace OptiLifts.Domain.Workouts;

public class WorkoutSetLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LogId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid? SetId { get; set; } //nullable
    public SetType Type { get; set; }
    public int Reps { get; set; }
    public float Weight { get; set; }
    public float Rpe { get; set; }
    public int OrderIndex { get; set; }
    public bool AiSuggested { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}