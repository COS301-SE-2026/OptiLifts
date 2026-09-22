using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ArenaMemberConfiguration : IEntityTypeConfiguration<ArenaMember>
{
    public void Configure(EntityTypeBuilder<ArenaMember> builder)
    {
        builder.ToTable("arena_members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("member_id");
        builder.Property(m => m.ArenaId).HasColumnName("arena_id").HasMaxLength(50).IsRequired();
        builder.Property(m => m.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(m => m.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("Member").IsRequired();
        builder.Property(m => m.JoinedAt).HasColumnName("joined_at").IsRequired();

        builder.HasOne<Arena>().WithMany().HasForeignKey(m => m.ArenaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new
        {
            m.ArenaId,
            m.UserId
        }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}