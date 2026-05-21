using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApp.Infrastructure.Configurations.Common;
using Task = TaskApp.Domain.Entities.Task;

namespace TaskApp.Infrastructure.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.ToTable("Task", "Core");
        builder.ApplyBaseEntityConfiguration();

        builder.HasKey(x => x.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Status).IsRequired();

        builder.HasOne(e => e.Creator)
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(e => e.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);

    }
}
