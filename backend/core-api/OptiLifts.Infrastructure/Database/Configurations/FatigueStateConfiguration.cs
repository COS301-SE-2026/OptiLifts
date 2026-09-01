using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Training;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class FatigueStateConfiguration : IEntityTypeConfiguration<FatigueState>
{
    public void Configure(EntityTypeBuilder<FatigueState> builder)
    {
        builder.ToTable("fatigue_states");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("fatigue_state_id");

        builder.Property(f => f.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(f => f.AcuteLoad).HasColumnName("acute_load").IsRequired();
        builder.Property(f => f.ChronicLoad).HasColumnName("chronic_load").IsRequired();
        builder.Property(f => f.Acwr).HasColumnName("acwr").IsRequired();
        builder.Property(f => f.RpeSlope).HasColumnName("rpe_slope").IsRequired();
        builder.Property(f => f.DecrementRatio).HasColumnName("decrement_ratio").IsRequired();
        builder.Property(f => f.SignalsFired).HasColumnName("signals_fired").IsRequired();
        builder.Property(f => f.IsFlagged).HasColumnName("is_flagged").IsRequired();
        builder.Property(f => f.Confidence).HasColumnName("confidence").HasConversion<string>().IsRequired();
        builder.Property(f => f.ComputedAt).HasColumnName("computed_at").IsRequired();

        builder.HasIndex(f => f.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
