namespace OptiLifts.Domain.Workouts;

public enum ExercisePrType
{
    MaxWeight,
    MaxSetVolume
}

public class ExercisePr
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid WorkoutLogSetId { get; set; }
    public ExercisePrType PrType { get; set; }
    public float PrValue { get; set; }
    public float AchievedWeight { get; set; }
    public int AchievedReps { get; set; }
}