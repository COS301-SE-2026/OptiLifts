using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Training;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ExerciseTrendConfiguration : IEntityTypeConfiguration<ExerciseTrend>
{
    public void Configure(EntityTypeBuilder<ExerciseTrend> builder)
    {
        builder.ToTable("exercise_trends");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("trend_id");

        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(t => t.ExerciseId).HasColumnName("exercise_id").IsRequired();
        builder.Property(t => t.SlopePctPerWeek).HasColumnName("slope_pct_per_week").IsRequired();
        builder.Property(t => t.SlopeCiLow).HasColumnName("slope_ci_low").IsRequired();
        builder.Property(t => t.SlopeCiHigh).HasColumnName("slope_ci_high").IsRequired();
        builder.Property(t => t.MeanE1rm).HasColumnName("mean_e1rm").IsRequired();
        builder.Property(t => t.SessionsUsed).HasColumnName("sessions_used").IsRequired();
        builder.Property(t => t.WindowStart).HasColumnName("window_start").IsRequired();
        builder.Property(t => t.WindowEnd).HasColumnName("window_end").IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(t => t.ComputedAt).HasColumnName("computed_at").IsRequired();
        builder.Property(t => t.SupersedesExerciseId).HasColumnName("supersedes_exercise_id");
        builder.Property(t => t.RpeTrendRising).HasColumnName("rpe_trend_rising").IsRequired();

        builder.HasIndex(t => new { t.UserId, t.ExerciseId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(t => t.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(t => t.SupersedesExerciseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
