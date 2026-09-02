using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Training;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class TrainingEventConfiguration : IEntityTypeConfiguration<TrainingEvent>
{
    public void Configure(EntityTypeBuilder<TrainingEvent> builder)
    {
        builder.ToTable("training_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("event_id");

        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>().IsRequired();
        builder.Property(e => e.Scope).HasColumnName("scope").IsRequired();
        builder.Property(e => e.Diagnosis).HasColumnName("diagnosis");
        builder.Property(e => e.Confidence).HasColumnName("confidence");
        builder.Property(e => e.Recommendation).HasColumnName("recommendation");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.AcknowledgedAt).HasColumnName("acknowledged_at");
        builder.Property(e => e.Outcome).HasColumnName("outcome");

        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
