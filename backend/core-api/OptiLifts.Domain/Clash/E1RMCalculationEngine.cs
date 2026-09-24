using OptiLifts.Domain.ProgressiveOverload;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Domain.Clash;

public static class E1RMCalculationEngine
{
    private const int MaxRepsForE1RM = 10;

    public static float CalculateE1RM(float weight, int reps)
    {
        if (weight <= 0f || reps <= 0)
        {
            return 0f;
        }

        var clampedReps = Math.Min(reps, MaxRepsForE1RM);

        var e1RM = E1RMCalculator.CalculateE1RM(
            weight,
            clampedReps,
            mechanic: "compound",
            exerciseType: ExerciseType.WeightReps);

        return (float)e1RM;
    }
}
