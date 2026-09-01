namespace OptiLifts.Domain.Training;

public class ExerciseTrend
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ExerciseId { get; set; }
    public float SlopePctPerWeek { get; set; }
    public float SlopeCiLow { get; set; }
    public float SlopeCiHigh { get; set; }
    public float MeanE1rm { get; set; }
    public int SessionsUsed { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public TrendStatus Status { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public Guid? SupersedesExerciseId { get; set; }
}

public enum TrendStatus
{
    Progressing,
    Regressing,
    Plateau,
    InsufficientData,
    InsufficientBaseline
}
