namespace OptiLifts.Domain.Gamification;

public enum BadgeCategory { Milestone, Streak, Strength, Consistency, Volume }

public class Badge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; //rule
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public BadgeCategory Category { get; set; }
    public int? Threshold { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}