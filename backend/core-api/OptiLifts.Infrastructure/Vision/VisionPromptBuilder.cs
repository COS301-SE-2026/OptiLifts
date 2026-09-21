using System.Collections.Generic;
using System.Linq;
using OptiLifts.Application.Vision;

namespace OptiLifts.Infrastructure.Vision;

public class VisionPromptBuilder : IVisionPromptBuilder
{
    public const string PositiveReinforcementMessage = "Clean reps! Your form looks solid, keep it up.";

    public string BuildPrompt(string exercise, IEnumerable<VisionAnomaly> anomalies)
    {
        var list = anomalies?
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Error))
            .OrderByDescending(a => a.Severity)
            .ToList() ?? new List<VisionAnomaly>();

        if (list.Count == 0)
        {
            return $"User {exercise} had clean form. Write a concise, actionable 2-sentence coaching tip encouraging the user and instructing proper form.";
        }

        var hasSeverity = list.Any(a => a.Severity < 0.999 || a.Severity > 1.001);
        if (hasSeverity)
        {
            var formatted = string.Join(", ", list.Select(a => $"{a.Error} (severity: {a.Severity:F2})"));
            return $"User {exercise} errors with severity scores: {formatted}. The error with the highest severity is the most critical. Write a concise, actionable 2-sentence coaching tip encouraging the user, prioritizing the most critical error first, and instructing proper form.";
        }

        var commaSeparated = string.Join(", ", list.Select(a => a.Error));
        return $"User {exercise} errors: {commaSeparated}. Write a concise, actionable 2-sentence coaching tip encouraging the user and instructing proper form.";
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
            return $"Great effort on your {exercise}! Maintain steady tempo and keep your core braced.";
        }

        var primary = list.First();
        return $"Great effort on your {exercise}! Prioritize fixing {primary.Error} and keep your reps controlled.";
    }

    public string GetFallbackCoachingTip(string exercise, IEnumerable<string>? detectedAnomalies = null)
    {
        var list = detectedAnomalies?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => new VisionAnomaly(a.Trim(), 1.0));

        return GetFallbackCoachingTip(exercise, list);
    }
}
