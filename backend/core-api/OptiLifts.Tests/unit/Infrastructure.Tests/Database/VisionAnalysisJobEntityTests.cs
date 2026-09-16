using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Vision;
using OptiLifts.Infrastructure.Database;
using Xunit;

namespace OptiLifts.Tests.Infrastructure.Tests.Database;

public class VisionAnalysisJobEntityTests
{
    [Fact]
    public void VisionAnalysisJob_DefaultValues_AreInitializedCorrectly()
    {
        // Act
        var job = new VisionAnalysisJob();

        // Assert
        job.JobId.Should().NotBeNullOrWhiteSpace();
        job.Status.Should().Be(VisionJobStatus.Pending);
        job.DetectedAnomalies.Should().NotBeNull();
        job.DetectedAnomalies.Should().BeEmpty();
        job.CoachSummary.Should().BeNull();
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        job.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DbContext_ModelConfiguration_ConfiguresVisionAnalysisJobCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy;Username=dummy;Password=dummy")
            .Options;

        using var context = new OptiLiftsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(VisionAnalysisJob));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("vision_analysis_jobs");

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Select(p => p.Name).Should().ContainSingle("JobId");

        var jobIdProp = entityType.FindProperty(nameof(VisionAnalysisJob.JobId));
        jobIdProp.Should().NotBeNull();
        jobIdProp!.GetColumnName().Should().Be("job_id");
        jobIdProp.GetMaxLength().Should().Be(128);
        jobIdProp.IsNullable.Should().BeFalse();

        var userIdProp = entityType.FindProperty(nameof(VisionAnalysisJob.UserId));
        userIdProp.Should().NotBeNull();
        userIdProp!.GetColumnName().Should().Be("user_id");
        userIdProp.GetMaxLength().Should().Be(128);
        userIdProp.IsNullable.Should().BeFalse();

        var statusProp = entityType.FindProperty(nameof(VisionAnalysisJob.Status));
        statusProp.Should().NotBeNull();
        statusProp!.GetColumnName().Should().Be("status");

        var anomaliesProp = entityType.FindProperty(nameof(VisionAnalysisJob.DetectedAnomalies));
        anomaliesProp.Should().NotBeNull();
        anomaliesProp!.GetColumnName().Should().Be("detected_anomalies");

        // Indexes
        var indexes = entityType.GetIndexes().ToList();
        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == nameof(VisionAnalysisJob.UserId)));
        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == nameof(VisionAnalysisJob.Status)));
    }
}
