using OptiLifts.Domain.Workouts;

namespace OptiLifts.Domain.ProgressiveOverload;

public static class E1RMCalculator
{
    private const float BodyweightRatio = 0.50f;

    public static double CalculateE1RM(float weight, int reps, string? mechanic, ExerciseType exerciseType, float bodyweight = 0f)
    {
        if (exerciseType == ExerciseType.BodyweightReps)
        {
            return CalculateEpley(GetLiftedBodyweight(bodyweight), reps);
        }

        if (exerciseType == ExerciseType.WeightedBodyweight)
        {
            return CalculateEpley(bodyweight + weight, reps);
        }

        if (IsCompound(mechanic))
        {
            return weight * (1 + (0.0333 * reps));
        }

        if (reps <= 7)
        {
            return weight / (1.0278 - 0.0278 * reps);
        }
        else
        {
            return 0.0333 * reps * weight + weight;
        }
    }

    //Returns reps
    public static int ReverseEpleyReps(double targetE1RM, float previousWeight, string? mechanic, ExerciseType exerciseType, float bodyweight = 0f)
    {
        if (targetE1RM <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetE1RM), "Target e1RM must be greater than 0.");
        }

        if (exerciseType == ExerciseType.BodyweightReps)
        {
            float liftedWeight = GetLiftedBodyweight(bodyweight);
            if (liftedWeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bodyweight), "Bodyweight must be greater than 0.");
            }

            return Math.Max(1, (int)Math.Round(ReverseEpleyRepsFromLifted(targetE1RM, liftedWeight)));
        }

        if (exerciseType == ExerciseType.WeightedBodyweight)
        {
            float liftedWeight = bodyweight + previousWeight;
            if (liftedWeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bodyweight), "Bodyweight plus added weight must be greater than 0.");
            }

            return Math.Max(1, (int)Math.Round(ReverseEpleyRepsFromLifted(targetE1RM, liftedWeight)));
        }

        if (previousWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(previousWeight), "Previous weight must be greater than 0.");
        }

        if (!IsCompound(mechanic))
        {
            double lowRepReps = (1.0278d - (previousWeight / targetE1RM)) / 0.0278d;
            if (lowRepReps > 0 && lowRepReps <= 7)
            {
                return Math.Max(1, (int)Math.Round(lowRepReps));
            }
        }

        double reps = ((targetE1RM / previousWeight) - 1d) / 0.0333d;
        return Math.Max(1, (int)Math.Round(reps));
    }

    //Returns weight
    public static float ReverseEpleyWeight(double targetE1RM, int targetReps, string? mechanic, ExerciseType exerciseType, float bodyweight = 0f)
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

        if (exerciseType == ExerciseType.WeightedBodyweight)
        {
            double totalLiftedWeight = targetE1RM / (1d + (targetReps / 30d));
            return (float)Math.Max(0d, totalLiftedWeight - bodyweight);
        }

        if (!IsCompound(mechanic) && targetReps <= 7)
        {
            double lowRepFactor = 1.0278d - (0.0278d * targetReps);
            return (float)Math.Max(0d, targetE1RM * lowRepFactor);
        }

        double weight = targetE1RM / (1d + (0.0333d * targetReps));
        return (float)Math.Max(0d, weight);
    }

    private static float GetLiftedBodyweight(float bodyweight)
    {
        return bodyweight * BodyweightRatio;
    }

    private static double CalculateEpley(float liftedWeight, int reps)
    {
        return liftedWeight * (1d + (reps / 30d));
    }

    private static double ReverseEpleyRepsFromLifted(double targetE1RM, float liftedWeight)
    {
        return ((targetE1RM / liftedWeight) - 1d) * 30d;
    }

    private static bool IsCompound(string? mechanic)
    {
        return string.Equals(mechanic, "compound", StringComparison.OrdinalIgnoreCase);
    }
}