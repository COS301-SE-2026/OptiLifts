using Microsoft.EntityFrameworkCore;

namespace OptiLifts.Infrastructure.Database;

public class OptiLiftsDbContext : DbContext
{
    public OptiLiftsDbContext(DbContextOptions<OptiLiftsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
