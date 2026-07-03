namespace OptiLifts.Domain.Workouts;

public class WorkoutExercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutId { get; set; }
    public Guid ExerciseId { get; set; }
    public int OrderIndex { get; set; }
    public Guid? GroupId { get; set; }
}