using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Vision;
using OptiLifts.Domain.Vision;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Vision;

public class GetVisionResultHandler : IRequestHandler<GetVisionResultQuery, VisionResultResponse?>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetVisionResultHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<VisionResultResponse?> Handle(GetVisionResultQuery query, CancellationToken cancellationToken)
    {
        if (query == null || string.IsNullOrWhiteSpace(query.JobId))
            return null;

        var job = await _dbContext.VisionAnalysisJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobId == query.JobId, cancellationToken);

        if (job == null)
            return null;

        if (job.Status == VisionJobStatus.Completed)
        {
            return new VisionResultResponse
            {
                Status = "completed",
                CoachSummary = job.CoachSummary
            };
        }

        if (job.Status == VisionJobStatus.Failed)
        {
            return new VisionResultResponse
            {
                Status = "failed",
                CoachSummary = job.CoachSummary ?? "Analysis failed."
            };
        }

        // Pending or Processing
        return new VisionResultResponse
        {
            Status = "processing",
            CoachSummary = null
        };
    }
}
