namespace OptiLifts.Domain.Workouts;

public class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Mechanic { get; set; }
    public string? Equipment { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> PrimaryMuscles { get; set; } = new();
    public List<string> SecondaryMuscles { get; set; } = new();
    public Guid? UserId { get; set; } // Null for public, populated for custom exercises
}
