using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.Extensions.Logging;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Vision;
using OptiLifts.Domain.Vision;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Vision;

public class AnalyzeVisionHandler : IRequestHandler<AnalyzeVisionCommand, string>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ServiceBusSender? _serviceBusSender;
    private readonly ILogger<AnalyzeVisionHandler>? _logger;

    public AnalyzeVisionHandler(
        OptiLiftsDbContext dbContext,
        IBlobStorageService blobStorageService,
        ServiceBusSender? serviceBusSender = null,
        ILogger<AnalyzeVisionHandler>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _serviceBusSender = serviceBusSender;
        _logger = logger;
    }

    public async Task<string> Handle(AnalyzeVisionCommand command, CancellationToken cancellationToken)
    {
        if (command == null || command.Request == null)
            throw new ArgumentNullException(nameof(command));

        var request = command.Request;
        var jobId = $"job_{Guid.NewGuid():N}";

        var jsonPayload = JsonSerializer.Serialize(request);
        var blobUrl = await _blobStorageService.SaveVisionAnalysisFramesAsync(jobId, jsonPayload, cancellationToken);

        var jobEntity = new VisionAnalysisJob
        {
            JobId = jobId,
            UserId = request.UserId,
            Exercise = request.Exercise,
            View = request.View,
            BlobUrl = blobUrl,
            Status = VisionJobStatus.Pending,
            DetectedAnomalies = new List<string>(),
            CoachSummary = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.VisionAnalysisJobs.Add(jobEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_serviceBusSender != null)
        {
            try
            {
                var messagePayload = new VisionServiceBusMessage
                {
                    JobId = jobId,
                    Exercise = request.Exercise,
                    View = request.View,
                    BlobUrl = blobUrl
                };

                var messageJson = JsonSerializer.Serialize(messagePayload);
                var sbMessage = new ServiceBusMessage(messageJson)
                {
                    ContentType = "application/json",
                    MessageId = jobId
                };

                await _serviceBusSender.SendMessageAsync(sbMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not publish message to Service Bus for job {JobId}", jobId);
            }
        }

        return jobId;
    }
}
