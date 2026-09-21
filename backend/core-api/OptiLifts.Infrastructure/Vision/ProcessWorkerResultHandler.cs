using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OptiLifts.Application.Vision;
using OptiLifts.Domain.Vision;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Vision;

public class ProcessWorkerResultHandler : IRequestHandler<ProcessWorkerResultCommand, bool>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IGeminiClient _geminiClient;
    private readonly ILogger<ProcessWorkerResultHandler>? _logger;

    public ProcessWorkerResultHandler(
        OptiLiftsDbContext dbContext,
        IGeminiClient geminiClient,
        ILogger<ProcessWorkerResultHandler>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _geminiClient = geminiClient ?? throw new ArgumentNullException(nameof(geminiClient));
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessWorkerResultCommand command, CancellationToken cancellationToken)
    {
        if (command == null || command.Result == null)
            throw new ArgumentNullException(nameof(command));

        var result = command.Result;
        var job = await _dbContext.VisionAnalysisJobs
            .FirstOrDefaultAsync(j => j.JobId == result.JobId, cancellationToken);

        if (job == null)
        {
            _logger?.LogWarning("VisionAnalysisJob with JobId '{JobId}' not found.", result.JobId);
            return false;
        }

        if (result.Success)
        {
            job.DetectedAnomalies = result.DetectedAnomalies ?? new List<string>();

            var coachingTip = await _geminiClient.GenerateCoachingTipAsync(
                job.Exercise,
                job.DetectedAnomalies,
                cancellationToken);

            job.CoachSummary = coachingTip;
            job.Status = VisionJobStatus.Completed;
        }
        else
        {
            job.Status = VisionJobStatus.Failed;
            job.CoachSummary = "Unable to complete form analysis due to an error during processing.";
        }

        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
