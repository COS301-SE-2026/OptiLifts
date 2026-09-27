namespace OptiLifts.Domain.Clash;

public class Arena
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Global"; //global, divisional or private
    public string? Code { get; set; } //globals and divisionals dont have a code
    public Guid? CreatedById { get; set; }
    public string MetricType { get; set; } = "DotsOverall"; //DotsOverall, TotalVolume, SquatE1RM, BenchE1RM, DeadliftsE1RM
    public int DurationDays { get; set; } = 30;
    public DateTime SeasonEndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}