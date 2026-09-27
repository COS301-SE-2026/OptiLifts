namespace OptiLifts.Domain.Clash;

public class Duel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public Guid ChallengerUserId { get; set; }
    public Guid RivalUserId { get; set; }
    public Guid? ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string TargetType { get; set; } = "E1RMGainPercent"; //E1RMGainPercent or TotalVolumeKg
    public int DurationDays { get; set; }
    public string Status { get; set; } = "Pending"; //Pending, Active, Finished, Declined
    public bool IsDraw { get; set; } = false;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? ChallengerBaselineValue { get; set; }
    public decimal? RivalBaselineValue { get; set; }
    public decimal ChallengerCurrentValue { get; set; } = 0m;
    public decimal RivalCurrentValue { get; set; } = 0m;
    public Guid? WinnerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
