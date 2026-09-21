using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OptiLifts.Application.Vision;

public class VisionAnalyzeRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("exercise")]
    public string Exercise { get; set; } = string.Empty;

    [JsonPropertyName("view")]
    public string View { get; set; } = string.Empty;

    [JsonPropertyName("frames")]
    public List<VisionFrame> Frames { get; set; } = new();
}

public class VisionFrame
{
    [JsonPropertyName("timestamp")]
    public double Timestamp { get; set; }

    [JsonPropertyName("landmarks")]
    public List<VisionLandmark> Landmarks { get; set; } = new();
}

public class VisionLandmark
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }
}

public class VisionServiceBusMessage
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("exercise")]
    public string Exercise { get; set; } = string.Empty;

    [JsonPropertyName("view")]
    public string View { get; set; } = string.Empty;

    [JsonPropertyName("blobUrl")]
    public string BlobUrl { get; set; } = string.Empty;
}

public class VisionWorkerResult
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("detected_anomalies")]
    public List<string> DetectedAnomalies { get; set; } = new();
}

public class VisionResultResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("coach_summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CoachSummary { get; set; }
}
