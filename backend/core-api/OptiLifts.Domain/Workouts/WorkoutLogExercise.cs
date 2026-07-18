namespace OptiLifts.Domain.Workouts;

public class WorkoutLogExercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LogId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid? WorkoutExerciseId { get; set; } //nullable
    public int OrderIndex { get; set; }
    public int GroupNumber { get; set; }
}
