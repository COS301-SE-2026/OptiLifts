namespace OptiLifts.Domain.Workouts;

public class Workout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? DayIndex { get; set; } //nullable
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}