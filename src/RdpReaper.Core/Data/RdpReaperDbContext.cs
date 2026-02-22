using Microsoft.EntityFrameworkCore;

namespace RdpReaper.Core.Data;

public sealed class RdpReaperDbContext : DbContext
{
    public RdpReaperDbContext(DbContextOptions<RdpReaperDbContext> options) : base(options)
    {
    }

    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<Ban> Bans => Set<Ban>();
    public DbSet<GeoCache> GeoCache => Set<GeoCache>();
    public DbSet<PolicyItem> Policy => Set<PolicyItem>();
    public DbSet<AuditLog> Audit => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
