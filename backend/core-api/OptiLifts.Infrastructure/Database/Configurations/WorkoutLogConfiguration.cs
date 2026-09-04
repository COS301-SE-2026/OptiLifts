using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class WorkoutLogConfiguration : IEntityTypeConfiguration<WorkoutLog>
{
    public void Configure(EntityTypeBuilder<WorkoutLog> builder)
    {
        builder.ToTable("workout_logs");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("log_id");

        builder.Property(w => w.EntryId).HasColumnName("entry_id");
        builder.Property(w => w.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(w => w.CompletedAt).HasColumnName("completed_at");
        builder.Property(w => w.AiModified).HasColumnName("ai_modified").IsRequired();
        builder.Property(w => w.Notes).HasColumnName("notes");

        //FK relationship to scheduled_entries (SetNull to keep log if entry deleted)
        builder.HasOne<ScheduledEntry>()
               .WithMany()
               .HasForeignKey(w => w.EntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}