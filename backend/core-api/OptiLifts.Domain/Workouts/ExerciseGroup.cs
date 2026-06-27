namespace OptiLifts.Domain.Workouts;

public class ExerciseGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutId { get; set; }
    public ExerciseGroupType Type { get; set; }
    public int Rounds { get; set; }
    public int RestTime { get; set; }
}

public enum ExerciseGroupType
{
    Superset,
    Circuit
}