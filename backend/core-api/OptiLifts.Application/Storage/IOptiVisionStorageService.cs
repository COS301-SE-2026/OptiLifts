using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OptiLifts.Application.Vision;

namespace OptiLifts.Application.Storage;

public interface IOptiVisionStorageService
{
    Task<string> SaveVisionAnalysisFramesAsync(string jobId, string jsonPayload, CancellationToken cancellationToken = default);

    Task<VisionServiceBusMessage> StageVisionAnalysisPayloadAsync(string jobId, VisionAnalyzeRequest request, CancellationToken cancellationToken = default);
    Task<VisionServiceBusMessage> StageVisionAnalysisPayloadAsync(string jobId, string exercise, string view, string jsonPayload, CancellationToken cancellationToken = default);

    Task<string?> GetVisionAnalysisFramesAsync(string jobId, CancellationToken cancellationToken = default);
    Task<Stream?> OpenVisionAnalysisFramesReadStreamAsync(string jobId, CancellationToken cancellationToken = default);
}
