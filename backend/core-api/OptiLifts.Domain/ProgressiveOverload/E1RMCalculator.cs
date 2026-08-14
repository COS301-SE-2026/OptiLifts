using OptiLifts.Domain.Workouts;

namespace OptiLifts.Domain.ProgressiveOverload;

public static class E1RMCalculator
{
    public static double CalculateE1RM(float weight, int reps, string? mechanic, ExerciseType exerciseType)
    {
        //don't normalize bodyweight
        if (exerciseType == ExerciseType.BodyweightReps)
        {
            return reps;
        }

        if (String.Equals(mechanic, "compound", StringComparison.OrdinalIgnoreCase))
        {
            return weight / ((52.2 + 41.9 * Math.Exp(-0.55 * reps)) / 100);
        }

        if (reps <= 7)
        {
            return weight / (1.0278 - 0.278 * reps);
        }
        else
        {
            return 0.33 * reps * weight + weight;
        }



    }

    //Returns reps
    public static int ReverseEpleyReps(double targetE1RM, float previousWeight, string? mechanic, ExerciseType exerciseType)
    {
        if (targetE1RM <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetE1RM), "Target e1RM must be greater than 0.");
        }

        if (exerciseType == ExerciseType.BodyweightReps)
        {
            return Math.Max(1, (int)Math.Round(targetE1RM));
        }

        if (previousWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(previousWeight), "Previous weight must be greater than 0.");
        }

        double reps = ((targetE1RM / previousWeight) - 1d) / 0.0333d;
        return Math.Max(1, (int)Math.Round(reps));
    }

    //Returns weight
    public static float ReverseEpleyWeight(double targetE1RM, int targetReps, string? mechanic, ExerciseType exerciseType)
    {
        if (targetE1RM <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetE1RM), "Target e1RM must be greater than 0.");
        }

        if (exerciseType == ExerciseType.BodyweightReps)
        {
            throw new InvalidOperationException("ReverseEpleyWeight is only valid for weighted exercises.");
        }

        if (targetReps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetReps), "Target reps must be greater than 0.");
        }

        double weight = targetE1RM / (1d + (0.0333d * targetReps));
        return (float)Math.Max(0d, weight);
    }
}