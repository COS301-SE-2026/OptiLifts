namespace OptiLifts.Domain.Workouts;

public enum SetType
{
    Normal,
    Superset,
    DropSet,
    Circuit
}

public class WorkoutSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutId { get; set; }
    public Guid ExerciseId { get; set; }
    public SetType Type { get; set; } = SetType.Normal;
    public int Reps { get; set; }
    public float Weight { get; set; }
    public int OrderIndex { get; set; }
    public int RestTime { get; set; }
}