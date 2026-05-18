using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class WorkoutLogSetConfiguration : IEntityTypeConfiguration<WorkoutSetLog>
{
    public void Configure(EntityTypeBuilder<WorkoutSetLog> builder)
    {
        builder.ToTable("workout_log_sets");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("log_set_id");

        builder.Property(w => w.LogId).HasColumnName("log_id").IsRequired();
        builder.Property(w => w.ExerciseId).HasColumnName("exercise_id").IsRequired();
        builder.Property(w => w.SetId).HasColumnName("set_id");
        builder.Property(w => w.Type).HasColumnName("set_type").HasConversion<string>().IsRequired();
        builder.Property(w => w.Reps).HasColumnName("reps").IsRequired();
        builder.Property(w => w.Weight).HasColumnName("weight").IsRequired();
        builder.Property(w => w.Rpe).HasColumnName("rpe").IsRequired();
        builder.Property(w => w.OrderIndex).HasColumnName("order_index").IsRequired();
        builder.Property(w => w.AiSuggested).HasColumnName("ai_suggested").IsRequired();
        builder.Property(w => w.LoggedAt).HasColumnName("logged_at").IsRequired();

        //FK relationship between workout log set and workout log
        builder.HasOne<WorkoutLog>()
               .WithMany()
               .HasForeignKey(w => w.LogId)
               .OnDelete(DeleteBehavior.Cascade);

        //FK relationship between workout log set and exercise
        builder.HasOne<Exercise>()
               .WithMany()
               .HasForeignKey(w => w.ExerciseId)
               .OnDelete(DeleteBehavior.Restrict);

        //FK relationship between workout log set and workout set
        builder.HasOne<WorkoutSet>()
               .WithMany()
               .HasForeignKey(w => w.SetId)
               .OnDelete(DeleteBehavior.SetNull); //if workout set is deleted keep the log set and set set id = null
    }
}