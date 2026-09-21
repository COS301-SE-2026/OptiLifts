using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class AthleteProfileKudosConfiguration : IEntityTypeConfiguration<AthleteProfileKudos>
{
    public void Configure(EntityTypeBuilder<AthleteProfileKudos> builder)
    {
        builder.ToTable("athlete_profile_kudos");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).HasColumnName("kudos_id");
        builder.Property(k => k.TargetUserId).HasColumnName("target_user_id").IsRequired();
        builder.Property(k => k.SenderUserId).HasColumnName("sender_user_id").IsRequired();
        builder.Property(k => k.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(k => k.TargetUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(k => k.SenderUserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(k => new
        {
            k.TargetUserId,
            k.SenderUserId
        }).IsUnique();
    }
}