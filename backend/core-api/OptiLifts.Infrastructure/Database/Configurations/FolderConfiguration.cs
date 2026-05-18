using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("folders");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("folder_id");

        builder.Property(f => f.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(f => f.Name).HasColumnName("name").IsRequired().HasMaxLength(150);
        builder.Property(f => f.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();

        //FK relationship betwen folder and user
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}