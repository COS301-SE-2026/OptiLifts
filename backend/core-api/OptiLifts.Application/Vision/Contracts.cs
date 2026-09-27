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

    [JsonPropertyName("start_frame")]
    public int StartFrame { get; set; }

    [JsonPropertyName("end_frame")]
    public int EndFrame { get; set; }

    public VisionAnomaly() { }

    public VisionAnomaly(string error, double severity = 1.0)
    {
        Error = error;
        Severity = severity;
    }

    public static implicit operator VisionAnomaly(string error) => new(error);
    public static implicit operator string(VisionAnomaly anomaly) => anomaly?.Error ?? string.Empty;

    public override string ToString() => $"{Error}|{StartFrame}";
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

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var anomaly = ParseStringAnomaly(reader.GetString());
                if (anomaly != null) list.Add(anomaly);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var anomaly = ParseObjectAnomaly(doc.RootElement);
                if (anomaly != null) list.Add(anomaly);
            }
        }

        return list;
    }

    private static VisionAnomaly? ParseStringAnomaly(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }
        else
        {
            return new VisionAnomaly(str.Trim(), 1.0);
        }
    }

    private static VisionAnomaly? ParseObjectAnomaly(JsonElement root)
    {
        string error = ExtractStringProperty(root, "error", "name");
        double severity = ExtractDoubleProperty(root, "severity", "confidence");

        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }
        else
        {
            return new VisionAnomaly(error.Trim(), severity);
        }
    }

    private static string ExtractStringProperty(JsonElement root, string primary, string fallback)
    {
        if (root.TryGetProperty(primary, out var p1) && p1.GetString() is string s1)
        {
            return s1;
        }
        if (root.TryGetProperty(fallback, out var p2) && p2.GetString() is string s2)
        {
            return s2;
        }
        return string.Empty;
    }

    private static double ExtractDoubleProperty(JsonElement root, string primary, string fallback)
    {
        if (root.TryGetProperty(primary, out var p1) && p1.TryGetDouble(out var d1))
        {
            return d1;
        }
        if (root.TryGetProperty(fallback, out var p2) && p2.TryGetDouble(out var d2))
        {
            return d2;
        }
        return 1.0;
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

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("issues")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Issues { get; set; }
}
