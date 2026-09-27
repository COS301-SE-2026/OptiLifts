using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class DuelConfiguration : IEntityTypeConfiguration<Duel>
{
    public void Configure(EntityTypeBuilder<Duel> builder)
    {
        builder.ToTable("duels");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("duel_id");
        builder.Property(d => d.Title).HasColumnName("title").HasMaxLength(120).IsRequired();
        builder.Property(d => d.ChallengerUserId).HasColumnName("challenger_user_id").IsRequired();
        builder.Property(d => d.RivalUserId).HasColumnName("rival_user_id").IsRequired();
        builder.Property(d => d.ExerciseId).HasColumnName("exercise_id");
        builder.Property(d => d.ExerciseName).HasColumnName("exercise_name").HasMaxLength(100).IsRequired();
        builder.Property(d => d.TargetType).HasColumnName("target_type").HasMaxLength(30).IsRequired();
        builder.Property(d => d.DurationDays).HasColumnName("duration_days").IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
        builder.Property(d => d.IsDraw).HasColumnName("is_draw").HasDefaultValue(false);
        builder.Property(d => d.StartDate).HasColumnName("start_date");
        builder.Property(d => d.EndDate).HasColumnName("end_date");
        builder.Property(d => d.ChallengerBaselineValue).HasColumnName("challenger_baseline_value").HasPrecision(10, 2);
        builder.Property(d => d.RivalBaselineValue).HasColumnName("rival_baseline_value").HasPrecision(10, 2);
        builder.Property(d => d.ChallengerCurrentValue).HasColumnName("challenger_current_value").HasPrecision(10, 2).HasDefaultValue(0m);
        builder.Property(d => d.RivalCurrentValue).HasColumnName("rival_current_value").HasPrecision(10, 2).HasDefaultValue(0m);
        builder.Property(d => d.WinnerUserId).HasColumnName("winner_user_id");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(d => d.ChallengerUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.RivalUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.WinnerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Exercise>().WithMany().HasForeignKey(d => d.ExerciseId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => new { d.ChallengerUserId, d.Status });
        builder.HasIndex(d => new { d.RivalUserId, d.Status });
        builder.HasIndex(d => new { d.Status, d.EndDate }).HasFilter("status = 'Active'");
    }
}
