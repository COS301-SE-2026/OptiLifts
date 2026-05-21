namespace OptiLifts.Domain.Workouts;

public class Folder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } //nullable
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}