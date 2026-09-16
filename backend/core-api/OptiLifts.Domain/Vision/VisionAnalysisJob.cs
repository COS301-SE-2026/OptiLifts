using System;
using System.Collections.Generic;

namespace OptiLifts.Domain.Vision;

public class VisionAnalysisJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Exercise { get; set; } = string.Empty;
    public string View { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public VisionJobStatus Status { get; set; } = VisionJobStatus.Pending;
    public List<string> DetectedAnomalies { get; set; } = new();
    public string? CoachSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
