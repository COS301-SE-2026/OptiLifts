using System.ComponentModel.DataAnnotations.Schema;

namespace OptiLifts.Domain.Workouts;

public class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Mechanic { get; set; }
    public string? Equipment { get; set; }
    public ExerciseType ExerciseType { get; set; }
    public Guid PrimaryMuscleId { get; set; }
    public Guid? UserId { get; set; } // Null for public, populated for custom exercises
    public string? ImageUrl { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public enum ExerciseType
{
    WeightReps,
    BodyweightReps,
    AssistedWeightReps,
    WeightedBodyweight,
    Duration,
    DurationWeight,
    DistanceDuration,
    WeightDistance
}