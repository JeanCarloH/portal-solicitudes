using Microsoft.EntityFrameworkCore;
using Solicitudes.Core.Domain;

namespace Solicitudes.Core.Data;

public class SolicitudesDbContext : DbContext
{
    public SolicitudesDbContext(DbContextOptions<SolicitudesDbContext> options) : base(options)
    {
    }

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("ServiceRequests");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.ExternalReference).HasMaxLength(100).IsRequired();
            entity.HasIndex(r => r.ExternalReference).IsUnique();

            entity.Property(r => r.CustomerId).HasMaxLength(50).IsRequired();
            entity.Property(r => r.ServiceCode).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(500).IsRequired();

            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();
            entity.HasIndex(r => r.Status);

            entity.Property(r => r.LastError).HasMaxLength(1000);
        });
    }
}