using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OptiLifts.Application.Vision;

public interface IGeminiClient
{
    Task<string> GenerateCoachingTipAsync(
        string exercise,
        IEnumerable<string> detectedAnomalies,
        CancellationToken cancellationToken = default);
}
