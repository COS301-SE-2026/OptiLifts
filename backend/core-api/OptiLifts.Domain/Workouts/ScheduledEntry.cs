namespace OptiLifts.Domain.Workouts;

public class ScheduledEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutId { get; set; }
    public Guid UserId { get; set; }
    public DateTime Scheduled { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
}

public enum ScheduleStatus
{
    Scheduled,
    Completed,
    Missed,
    AdHoc
}