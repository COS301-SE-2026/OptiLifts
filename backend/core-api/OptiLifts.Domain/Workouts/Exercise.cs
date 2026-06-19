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

    //legacy - Not in the DB but kept only until until old code has been rewired
    [NotMapped] public string Category { get; set; } = string.Empty;
    [NotMapped] public List<string> PrimaryMuscles { get; set; } = new();
    [NotMapped] public List<string> SecondaryMuscles { get; set; } = new();
}

public enum ExerciseType
{
    WeightReps,
    BodyweightReps,
    AssistedWeightReps,
    Duration,
    DistanceDuration,
    WeightDistance
}