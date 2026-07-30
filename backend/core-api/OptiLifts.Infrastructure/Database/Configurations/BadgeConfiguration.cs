using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Gamification;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("badges");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("badge_id");

        builder.Property(b => b.Code).HasColumnName("code").IsRequired().HasMaxLength(64);
        builder.Property(b => b.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(b => b.Description).HasColumnName("description").IsRequired().HasMaxLength(255);
        builder.Property(b => b.IconUrl).HasColumnName("icon_url"); //nullable
        builder.Property(b => b.Category).HasColumnName("category").HasConversion<string>().IsRequired();
        builder.Property(b => b.Threshold).HasColumnName("threshold"); //nullable
        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(b => b.Name).IsUnique();
    }
}