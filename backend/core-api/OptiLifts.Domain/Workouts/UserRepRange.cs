namespace OptiLifts.Domain.Workouts;

public enum UserRepRangeExerciseType
{
    Compound,
    Isolation
}

public class UserRepRange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserRepRangeExerciseType ExerciseType { get; set; }
    public int LowerLimit { get; set; }
    public int UpperLimit { get; set; }
}
