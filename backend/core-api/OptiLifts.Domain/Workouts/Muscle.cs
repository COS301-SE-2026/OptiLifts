namespace OptiLifts.Domain.Workouts;

public class Muscle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}