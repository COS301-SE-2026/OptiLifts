using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Vision;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class VisionAnalysisJobConfiguration : IEntityTypeConfiguration<VisionAnalysisJob>
{
    public void Configure(EntityTypeBuilder<VisionAnalysisJob> builder)
    {
        builder.ToTable("vision_analysis_jobs");

        builder.HasKey(j => j.JobId);
        builder.Property(j => j.JobId)
            .HasColumnName("job_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(j => j.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(j => j.Exercise)
            .HasColumnName("exercise")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(j => j.View)
            .HasColumnName("view")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(j => j.BlobUrl)
            .HasColumnName("blob_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(VisionJobStatus.Pending)
            .IsRequired();

        builder.Property(j => j.DetectedAnomalies)
            .HasColumnName("detected_anomalies")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(j => j.CoachSummary)
            .HasColumnName("coach_summary")
            .IsRequired(false);

        builder.Property(j => j.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(j => j.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(j => j.Id);
        builder.Ignore(j => j.ExerciseType);

        builder.HasIndex(j => j.UserId);
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => j.Status);
    }
}
