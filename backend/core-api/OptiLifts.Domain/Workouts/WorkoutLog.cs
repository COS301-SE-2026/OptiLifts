namespace OptiLifts.Domain.Workouts;

public class WorkoutLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? EntryId { get; set; } //nullable, for the schedule?
    public Guid? WorkoutId { get; set; } //nullable 
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; } //nullable
    public bool AiModified { get; set; }
    public string? Notes { get; set; } //nullable
}