using System.Collections.Generic;

namespace OptiLifts.Application.Vision;

public interface IVisionPromptBuilder
{
    string BuildPrompt(string exercise, IEnumerable<VisionAnomaly> anomalies);
    string BuildPrompt(string exercise, IEnumerable<string> detectedAnomalies);
    string GetPositiveReinforcement();
    string GetFallbackCoachingTip(string exercise, IEnumerable<VisionAnomaly>? anomalies = null);
    string GetFallbackCoachingTip(string exercise, IEnumerable<string>? detectedAnomalies = null);
}
