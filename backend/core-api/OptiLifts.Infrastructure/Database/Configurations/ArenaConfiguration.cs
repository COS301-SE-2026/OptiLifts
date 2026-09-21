using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ArenaConfiguration : IEntityTypeConfiguration<Arena>
{
    public void Configure(EntityTypeBuilder<Arena> builder)
    {
        builder.ToTable("arenas");
        
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("arena_id").HasMaxLength(50);
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Code).HasColumnName("code").HasMaxLength(10);
        builder.Property(a => a.CreatedById).HasColumnName("created_by_id");
        builder.Property(a => a.MetricType).HasColumnName("metric_type").HasMaxLength(30).IsRequired();
        builder.Property(a => a.DurationDays).HasColumnName("duration_days").HasDefaultValue(30);
        builder.Property(a => a.SeasonEndDate).HasColumnName("season_end_date").IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.CreatedById).OnDelete(DeleteBehavior.SetNull);
        
        builder.HasIndex(a => a.Code).IsUnique().HasFilter("code IS NOT NULL");

        builder.HasIndex(a => a.Type);
    }
}