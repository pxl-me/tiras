using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Infrastructure;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<AnomalyRecord> Anomalies => Set<AnomalyRecord>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AnomalyRecord>(e =>
        {
            e.ToTable("anomalies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.RawJson).HasColumnType("jsonb");
        });
    }
}
