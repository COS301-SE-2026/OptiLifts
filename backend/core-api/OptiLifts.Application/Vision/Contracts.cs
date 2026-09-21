using System;
using System.Collections.Generic;
using System.Text.Json;
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

public class VisionAnomaly
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public double Severity { get; set; } = 1.0;

    public VisionAnomaly() { }

    public VisionAnomaly(string error, double severity = 1.0)
    {
        Error = error;
        Severity = severity;
    }

    public static implicit operator VisionAnomaly(string error) => new(error);
    public static implicit operator string(VisionAnomaly anomaly) => anomaly?.Error ?? string.Empty;

    public override string ToString() => Severity < 0.999 ? $"{Error} ({Severity:F2})" : Error;
}

public class VisionAnomalyListJsonConverter : JsonConverter<List<VisionAnomaly>>
{
    public override List<VisionAnomaly> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<VisionAnomaly>();

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            return list;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    list.Add(new VisionAnomaly(str.Trim(), 1.0));
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                string error = string.Empty;
                double severity = 1.0;

                if (root.TryGetProperty("error", out var errorProp))
                {
                    error = errorProp.GetString() ?? string.Empty;
                }
                else if (root.TryGetProperty("name", out var nameProp))
                {
                    error = nameProp.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("severity", out var severityProp) && severityProp.TryGetDouble(out var s))
                {
                    severity = s;
                }
                else if (root.TryGetProperty("confidence", out var confProp) && confProp.TryGetDouble(out var c))
                {
                    severity = c;
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    list.Add(new VisionAnomaly(error.Trim(), severity));
                }
            }
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<VisionAnomaly> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStartObject();
            writer.WriteString("error", item.Error);
            writer.WriteNumber("severity", item.Severity);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}

public class VisionWorkerResult
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("detected_anomalies")]
    [JsonConverter(typeof(VisionAnomalyListJsonConverter))]
    public List<VisionAnomaly> DetectedAnomalies { get; set; } = new();
}

public class VisionResultResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("coach_summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CoachSummary { get; set; }
}
