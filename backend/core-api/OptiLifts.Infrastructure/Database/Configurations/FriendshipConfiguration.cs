using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships", t => t.HasCheckConstraint("CK_friendships_user_order", "user_id1 < user_id2"));

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("friendship_id");
        builder.Property(f => f.UserId1).HasColumnName("user_id1").IsRequired();
        builder.Property(f => f.UserId2).HasColumnName("user_id2").IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(f => f.UserId1).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(f => f.UserId2).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new
        {
            f.UserId1,
            f.UserId2
        }).IsUnique(); //no dupe friendships
        builder.HasIndex(f => f.UserId2);
    }
}