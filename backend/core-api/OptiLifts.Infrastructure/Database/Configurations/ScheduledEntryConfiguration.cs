using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ScheduledEntryConfiguration : IEntityTypeConfiguration<ScheduledEntry>
{
    public void Configure(EntityTypeBuilder<ScheduledEntry> builder)
    {
        builder.ToTable("scheduled_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("entry_id");

        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.WorkoutId).HasColumnName("workout_id").IsRequired();
        builder.Property(e => e.Scheduled).HasColumnName("scheduled").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().IsRequired();

        //workout FK
        builder.HasOne<Workout>()
                .WithMany()
                .HasForeignKey(e => e.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

        //user owner FK
        builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}