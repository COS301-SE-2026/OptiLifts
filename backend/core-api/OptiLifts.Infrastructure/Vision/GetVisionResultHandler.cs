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

    private static int CalculateScore(List<string> anomalies)
    {
        if (anomalies == null || anomalies.Count == 0) return 100; //yipee
        double bad = 0;
        foreach (var a in anomalies)
        {
            var start = a.IndexOf('(');
            var stop = a.IndexOf(')');
            if ((start != -1) && (stop != -1) && (stop > start))
            {
                if (double.TryParse(a.Substring(start + 1, stop - start - 1), out var val))
                {
                    bad += val;
                }
            }
            else 
            {
                bad += 1.0;
            }
        }
        var score = (int)(100 - (bad * 15));
        return Math.Max(0, score);
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
            var issuesList = new List<string>();
            if (job.DetectedAnomalies != null)
            {
                foreach (var a in job.DetectedAnomalies)
                {
                    var name = a;
                    var braceIndex = name.IndexOf('(');
                    if (braceIndex != -1) name = name.Substring(0, braceIndex).Trim();
                    name = name.Replace('_', ' ');
                    name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                    issuesList.Add(name);
                }
            }

            return new VisionResultResponse
            {
                Status = "completed",
                CoachSummary = job.CoachSummary,
                Score = CalculateScore(job.DetectedAnomalies),
                Issues = issuesList
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
