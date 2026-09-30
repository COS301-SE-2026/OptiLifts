namespace OptiLifts.Domain.Clash;

public class DuelTimelineEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DuelId { get; set; }
    public Guid UserId { get; set; }
    public Guid? WorkoutLogSetId { get; set; }
    public string EventText { get; set; } = string.Empty;
    public bool IsPr { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
