using OptiLifts.Domain.Workouts;

namespace OptiLifts.Application.Workouts.CreateWorkout;

public static class CreateWorkoutValidator
{
    public static IReadOnlyList<string> Validate(CreateWorkoutRequest request)
    {
        var errors = new List<string>();
        var groups = request.Groups ?? [];
        var exercises = request.Exercises ?? [];

        var seenKeys = new HashSet<string>();

        foreach (var group in groups)
        {
            if (!seenKeys.Add(group.GroupKey))
            {
                errors.Add($"Duplicate group key for '{group.GroupKey}'.");
                continue;
            }

            if (!Enum.TryParse<ExerciseGroupType>(group.Type, ignoreCase: true, out var type))
            {
                errors.Add($"Group '{group.GroupKey}' type is '{group.Type}', which isn't valid.");
                continue;
            }

            if (group.Rounds < 1)
            {
                errors.Add($"Group '{group.GroupKey}' must have at least 1 round.");
            }

            if (group.RestTime < 0)
            {
                errors.Add($"Group '{group.GroupKey}' must be a positive number.");
            }

            var memberCount = exercises.Count(e => e.GroupKey == group.GroupKey);

            if (type == ExerciseGroupType.Superset && memberCount != 2)
            {
                errors.Add($"Supersets must be exactly two exercises (found {memberCount}).");
            }

            if (type == ExerciseGroupType.Circuit && memberCount < 3)
            {
                errors.Add($"Circuits must have atleast 3 exercises (found {memberCount}).");
            }
        }

        foreach (var exercise in exercises)
        {
            if (exercise.GroupKey is not null && !seenKeys.Contains(exercise.GroupKey))
            {
                errors.Add($"Exercise references an invalid group key: '{exercise.GroupKey}'.");
            }
        }

        return errors;
    }
}