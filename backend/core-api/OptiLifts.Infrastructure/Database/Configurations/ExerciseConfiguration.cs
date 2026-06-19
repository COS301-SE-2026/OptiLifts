using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("exercise_dictionary");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("exercise_dict_id");

        builder.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(e => e.Mechanic).HasColumnName("mechanic");
        builder.Property(e => e.Equipment).HasColumnName("equipment");
        builder.Property(e => e.ExerciseType).HasColumnName("exercise_type").HasConversion<string>().IsRequired();
        builder.Property(e => e.PrimaryMuscleId).HasColumnName("primary_muscle").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ImageUrl).HasColumnName("image_url");

        //primary muscle FK
        builder.HasOne<Muscle>()
                .WithMany()
                .HasForeignKey(e => e.PrimaryMuscleId)
                .OnDelete(DeleteBehavior.Restrict);

        //user owner FK (nullable for public exercises)
        builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}