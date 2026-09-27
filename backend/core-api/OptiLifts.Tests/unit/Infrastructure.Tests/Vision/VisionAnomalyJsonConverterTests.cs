using System.Text.Json;
using FluentAssertions;
using OptiLifts.Application.Vision;
using Xunit;

namespace OptiLifts.Tests.Unit.Infrastructure.Tests.Vision;

public class VisionAnomalyJsonConverterTests
{
    [Fact]
    public void Deserialize_WorkerResult_WithObjectAnomalies_ParsesCorrectly()
    {
        const string json = @"{
            ""jobId"": ""12345"",
            ""success"": true,
            ""detected_anomalies"": [
                {
                    ""error"": ""excessive_forward_lean"",
                    ""severity"": 0.98
                },
                {
                    ""error"": ""shallow_depth"",
                    ""severity"": 0.82
                }
            ]
        }";

        var result = JsonSerializer.Deserialize<VisionWorkerResult>(json);

        result.Should().NotBeNull();
        result!.JobId.Should().Be("12345");
        result.Success.Should().BeTrue();
        result.DetectedAnomalies.Should().HaveCount(2);
        result.DetectedAnomalies[0].Error.Should().Be("excessive_forward_lean");
        result.DetectedAnomalies[0].Severity.Should().Be(0.98);
        result.DetectedAnomalies[1].Error.Should().Be("shallow_depth");
        result.DetectedAnomalies[1].Severity.Should().Be(0.82);
    }

    [Fact]
    public void Deserialize_WorkerResult_WithStringAnomalies_ParsesCorrectly()
    {
        const string json = @"{
            ""jobId"": ""12345"",
            ""success"": true,
            ""detected_anomalies"": [
                ""excessive_forward_lean"",
                ""shallow_depth""
            ]
        }";

        var result = JsonSerializer.Deserialize<VisionWorkerResult>(json);

        result.Should().NotBeNull();
        result!.JobId.Should().Be("12345");
        result.Success.Should().BeTrue();
        result.DetectedAnomalies.Should().HaveCount(2);
        result.DetectedAnomalies[0].Error.Should().Be("excessive_forward_lean");
        result.DetectedAnomalies[0].Severity.Should().Be(1.0);
        result.DetectedAnomalies[1].Error.Should().Be("shallow_depth");
        result.DetectedAnomalies[1].Severity.Should().Be(1.0);
    }
}
