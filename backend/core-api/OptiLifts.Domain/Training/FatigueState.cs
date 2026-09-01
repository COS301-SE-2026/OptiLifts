namespace OptiLifts.Domain.Training;

public class FatigueState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public float AcuteLoad { get; set; }
    public float ChronicLoad { get; set; }
    public float Acwr { get; set; }
    public float RpeSlope { get; set; }
    public float DecrementRatio { get; set; }
    public int SignalsFired { get; set; }
    public bool IsFlagged { get; set; }
    public FatigueConfidence Confidence { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}

public enum FatigueConfidence
{
    Full,
    ReducedNoRpe
}
