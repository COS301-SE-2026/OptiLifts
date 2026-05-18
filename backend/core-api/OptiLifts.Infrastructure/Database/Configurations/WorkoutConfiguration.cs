using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> builder)
    {
        builder.ToTable("workouts");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("workout_id");

        builder.Property(w => w.FolderId).HasColumnName("folder_id").IsRequired();
        builder.Property(w => w.Name).HasColumnName("name").IsRequired().HasMaxLength(150);
        builder.Property(w => w.DayIndex).HasColumnName("day_index");
        builder.Property(w => w.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();

        //FK relationship between workout and folder
        builder.HasOne<Folder>()
               .WithMany()
               .HasForeignKey(w => w.FolderId)
               .OnDelete(DeleteBehavior.Cascade); //if the folder is deleted the associated workouts are deleted

        //FK relationship between workout and user 
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(w => w.CreatedBy)
               .OnDelete(DeleteBehavior.Restrict); //restrict as if we delete a user the folder and user will try delete it 
    }
}