using OptiLifts.Domain.Common;

namespace OptiLifts.Domain.Workouts;

public class WorkoutLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? EntryId { get; set; } //nullable, for the schedule?
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; } //nullable
    public bool AiModified { get; set; }

    [Encrypted]
    public string? Notes { get; set; } //nullable
}