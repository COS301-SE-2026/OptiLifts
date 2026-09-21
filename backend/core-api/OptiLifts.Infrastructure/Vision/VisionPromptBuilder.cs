using System.Collections.Generic;
using System.Linq;
using OptiLifts.Application.Vision;

namespace OptiLifts.Infrastructure.Vision;

public class VisionPromptBuilder : IVisionPromptBuilder
{
    public const string PositiveReinforcementMessage = "Clean reps! Your form looks solid, keep it up.";

    public string BuildPrompt(string exercise, IEnumerable<string> detectedAnomalies)
    {
        var anomaliesList = detectedAnomalies?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .ToList() ?? new List<string>();

        var commaSeparated = string.Join(", ", anomaliesList);
        return $"User {exercise} errors: {commaSeparated}. Write a concise, actionable 2-sentence coaching tip encouraging the user and instructing proper form.";
    }

    public string GetPositiveReinforcement() => PositiveReinforcementMessage;

    public string GetFallbackCoachingTip(string exercise, IEnumerable<string>? detectedAnomalies = null)
    {
        var anomaliesList = detectedAnomalies?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .ToList();

        if (anomaliesList == null || anomaliesList.Count == 0)
        {
            return $"Great effort on your {exercise}! Maintain steady tempo and keep your core braced.";
        }

        var commaSeparated = string.Join(", ", anomaliesList);
        return $"Great effort on your {exercise}! Stay mindful of {commaSeparated} and keep your reps controlled.";
    }
}
