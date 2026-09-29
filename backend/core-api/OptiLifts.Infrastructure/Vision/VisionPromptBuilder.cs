using System.Collections.Generic;
using System.Linq;
using OptiLifts.Application.Vision;

namespace OptiLifts.Infrastructure.Vision;

public class VisionPromptBuilder : IVisionPromptBuilder
{
    public const string PositiveReinforcementMessage = "Clean reps! Your form looks solid, keep it up.";

    private static readonly Dictionary<string, string> ErrorDefinitions = new(System.StringComparer.OrdinalIgnoreCase)
    {
        // squat
        { "shallow_depth", "Lower until the crease of your hip is below the top of your kneecap, shallow squats are flagged." },
        { "excessive_forward_lean", "Keep your chest up and your torso upright, folding forward like a good morning is flagged." },
        
        // deadlift
        { "lumbar_flexion", "Keep your spine neutral and straight through the pull, a rounded back is flagged." },
        { "bad_hip_movement", "Start with your hips low and raise them simultaneously with your back, hips starting too high or shooting up early are flagged." },
        { "bar_drifting", "Start with the bar over the middle of your foot and drag it up your shins, a bar drifting away from your legs is flagged." },
        { "knees_forward", "Keep your knees from travelling far over your toes at the start, knees pushed too far forward are flagged." },

        // bench
        { "excessive_elbow_flare", "Keep your elbows tucked in, elbows flared out to 90 degrees from your torso are flagged." },
        { "no_chest_touch", "Lower the bar all the way to your chest, half reps that never touch are flagged." },
        { "incorrect_bar_path", "Touch the bar to your mid to lower sternum, touching near your neck or down at your stomach are flagged." },
        { "bad_arch", "Keep a slight natural arch with your chest up and shoulder blades pinched together, lying completely flat is flagged." }
    };

    public string BuildPrompt(string exercise, IEnumerable<VisionAnomaly> anomalies)
    {
        var list = anomalies?
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Error))
            .OrderByDescending(a => a.Severity)
            .ToList() ?? new List<VisionAnomaly>();

        if (list.Count == 0)
        {
            return $"Act as an expert gym coach. The user just performed a {exercise} and had perfect form with no errors.\n\nWrite a concise, actionable 1-2 sentence coaching tip encouraging them to keep up the great work. Do not use emojis, hashtags, or corporate speak. Be direct and encouraging.";
        }

        var formattedErrors = string.Join(", ", list.Select(a => $"{a.Error} (severity: {a.Severity:F2})"));

        var definitions = list
            .Where(a => ErrorDefinitions.ContainsKey(a.Error))
            .Select(a => $"- {a.Error}: {ErrorDefinitions[a.Error]}")
            .ToList();

        var definitionsBlock = definitions.Count > 0
            ? $"\n\nFor context, here is how our system defines these errors:\n{string.Join("\n", definitions)}"
            : "";

        return $@"Act as an expert gym coach. The user just performed a {exercise} and the following errors were detected by our motion-tracking system: {formattedErrors}.
    
        The error with the highest severity is the most critical. Write a concise, actionable coaching tip. The errors mean the following in our system:{definitionsBlock}

        Rules:
        1. Prioritize fixing the most critical error first.
        2. Do not just state the error. Provide a practical physical cue to fix it (e.g., 'Push the floor away').
        3. Do not use emojis, hashtags, or corporate speak. Be direct and encouraging.
        4. Keep it strictly to 2-3 sentences.";
    }

    public string BuildPrompt(string exercise, IEnumerable<string> detectedAnomalies)
    {
        var anomaliesList = detectedAnomalies?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => new VisionAnomaly(a.Trim(), 1.0))
            .ToList() ?? new List<VisionAnomaly>();

        return BuildPrompt(exercise, anomaliesList);
    }

    public string GetPositiveReinforcement() => PositiveReinforcementMessage;

    public string GetFallbackCoachingTip(string exercise, IEnumerable<VisionAnomaly>? anomalies = null)
    {
        var list = anomalies?
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Error))
            .OrderByDescending(a => a.Severity)
            .ToList();

        if (list == null || list.Count == 0)
        {
            return $"Good effort on your {exercise}! Maintain steady tempo and keep your core braced.";
        }

        var formattedErrors = list.Select(a => FormatAnomalyName(a.Error));
        return $"Good effort on your {exercise}! We identified the following issues in your lift: {string.Join(", ", formattedErrors)}.";
    }

    public string GetFallbackCoachingTip(string exercise, IEnumerable<string>? detectedAnomalies = null)
    {
        var list = detectedAnomalies?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => new VisionAnomaly(a.Trim(), 1.0));

        return GetFallbackCoachingTip(exercise, list);
    }

    private static string FormatAnomalyName(string error)
    {
        if (string.IsNullOrWhiteSpace(error)) return error;
        var words = error.Split('_', System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }
}
