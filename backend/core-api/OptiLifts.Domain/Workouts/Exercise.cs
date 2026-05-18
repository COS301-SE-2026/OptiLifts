namespace OptiLifts.Domain.Workouts;

public class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Mechanic { get; set; } //nullable
    public string? Equipment { get; set; } //nullable
    public string Category { get; set; } = string.Empty;
    public List<string> PrimaryMuscles { get; set; } = new();
    public List<string> SecondaryMuscles { get; set; } = new();
}