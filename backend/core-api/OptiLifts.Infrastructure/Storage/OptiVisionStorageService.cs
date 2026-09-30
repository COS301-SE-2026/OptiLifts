using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Vision;

namespace OptiLifts.Infrastructure.Storage;

public class OptiVisionStorageService : IOptiVisionStorageService
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly BlobServiceClient _blobServiceClient;

    public OptiVisionStorageService(IBlobStorageService blobStorageService, BlobServiceClient blobServiceClient)
    {
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
    }

    public Task<string> SaveVisionAnalysisFramesAsync(string jobId, string jsonPayload, CancellationToken cancellationToken = default)
        => _blobStorageService.SaveVisionAnalysisFramesAsync(jobId, jsonPayload, cancellationToken);

    public async Task<VisionServiceBusMessage> StageVisionAnalysisPayloadAsync(
        string jobId,
        VisionAnalyzeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var json = JsonSerializer.Serialize(request);
        return await StageVisionAnalysisPayloadAsync(jobId, request.Exercise, request.View, json, cancellationToken);
    }

    public async Task<VisionServiceBusMessage> StageVisionAnalysisPayloadAsync(
        string jobId,
        string exercise,
        string view,
        string jsonPayload,
        CancellationToken cancellationToken = default)
    {
        var blobUrl = await SaveVisionAnalysisFramesAsync(jobId, jsonPayload, cancellationToken);

        return new VisionServiceBusMessage
        {
            JobId = jobId,
            Exercise = exercise,
            View = view,
            BlobUrl = blobUrl
        };
    }

    public Task<string?> GetVisionAnalysisFramesAsync(string jobId, CancellationToken cancellationToken = default)
        => _blobServiceClient.GetVisionAnalysisFramesAsync(jobId, cancellationToken);

    public Task<Stream?> OpenVisionAnalysisFramesReadStreamAsync(string jobId, CancellationToken cancellationToken = default)
        => _blobServiceClient.OpenVisionAnalysisFramesReadStreamAsync(jobId, cancellationToken);
}
