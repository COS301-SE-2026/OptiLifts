using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class DuelTimelineEventConfiguration : IEntityTypeConfiguration<DuelTimelineEvent>
{
    public void Configure(EntityTypeBuilder<DuelTimelineEvent> builder)
    {
        builder.ToTable("duel_timeline_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("timeline_event_id");
        builder.Property(e => e.DuelId).HasColumnName("duel_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.WorkoutLogSetId).HasColumnName("workout_log_set_id");
        builder.Property(e => e.EventText).HasColumnName("event_text").HasMaxLength(255).IsRequired();
        builder.Property(e => e.IsPr).HasColumnName("is_pr").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<Duel>().WithMany().HasForeignKey(e => e.DuelId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<WorkoutSetLog>().WithMany().HasForeignKey(e => e.WorkoutLogSetId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.DuelId, e.CreatedAt });
    }
}
