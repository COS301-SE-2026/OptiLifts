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
            return weight /((52.2+41.9* Math.Exp(-0.55*reps))/100);
        }

        if (reps <= 7)
        {
            return weight / (1.0278-0.278*reps);
        }
        else
        {
            return 0.33*reps*weight + weight;
        }
        


    }
}