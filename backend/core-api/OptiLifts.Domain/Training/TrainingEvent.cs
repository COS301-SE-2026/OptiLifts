namespace OptiLifts.Domain.Training;

public class TrainingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public TrainingEventType Type { get; set; }
    public string Scope { get; set; } = "GLOBAL";
    public string? Diagnosis { get; set; }
    public float? Confidence { get; set; }
    public string? Recommendation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public string? Outcome { get; set; }
}

public enum TrainingEventType
{
    AcuteFatigueFlagged,
    PlateauDetected,
    ChronicFatigueFlagged,
    DeloadSuggested,
    Resolved
}
