using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class WorkoutLogConfiguration : IEntityTypeConfiguration<WorkoutLog>
{
    public void Configure(EntityTypeBuilder<WorkoutLog> builder)
    {
        builder.ToTable("workout_logs");
        
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("log_id");

        builder.Property(w => w.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(w => w.EntryId).HasColumnName("entry_id");
        builder.Property(w => w.WorkoutId).HasColumnName("workout_id");
        builder.Property(w => w.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(w => w.CompletedAt).HasColumnName("completed_at");
        builder.Property(w => w.AiModified).HasColumnName("ai_modified").IsRequired();
        builder.Property(w => w.Notes).HasColumnName("notes");

        //FK relationship between workout log and user
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(w => w.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        //FK relationship between workout log and workout    
        builder.HasOne<Workout>()
               .WithMany()
               .HasForeignKey(w => w.WorkoutId)
               .OnDelete(DeleteBehavior.SetNull); //if a workout is deleted keep the log and set workout id= null
    }
}