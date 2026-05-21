using Microsoft.EntityFrameworkCore;
using TaskApp.Domain.Common;
using TaskApp.Domain.Entities;
using Task = TaskApp.Domain.Entities.Task;

namespace TaskApp.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Task> Tasks => Set<Task>();
    public DbSet<User> Users => Set<User>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var baseEntries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in baseEntries)
            if (entry.State == EntityState.Modified)
                entry.Entity.Touch();
    }
}
