namespace OptiLifts.Domain.Clash;

public class ClashActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ArenaId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string EventText { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool IsPr { get; set; } = false;
    public bool IsPromotion { get; set; } = false;
    public int KudosCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}