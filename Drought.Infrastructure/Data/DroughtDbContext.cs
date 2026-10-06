using Drought.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Drought.Infrastructure.Data;

/// <summary>
/// Contexto principal do Entity Framework Core. Herda de IdentityDbContext
/// para integrar directamente o ASP.NET Core Identity (utilizadores e roles).
/// </summary>
public class DroughtDbContext : IdentityDbContext<ApplicationUser>
{
    public DroughtDbContext(DbContextOptions<DroughtDbContext> options) : base(options) { }

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<SoilObservation> SoilObservations => Set<SoilObservation>();
    public DbSet<DatasetSource> DatasetSources => Set<DatasetSource>();
    public DbSet<ModelVersion> ModelVersions => Set<ModelVersion>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // necessário para as tabelas do Identity

        builder.Entity<Region>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Name).HasMaxLength(100).IsRequired();
        });

        builder.Entity<SoilObservation>(e =>
        {
            e.HasIndex(o => new { o.RegionId, o.Date });
            e.Property(o => o.S1_SoilMoisture).HasPrecision(9, 4);
            e.Property(o => o.Ascat_SoilMoisture).HasPrecision(9, 4);

            e.HasOne(o => o.Region)
                .WithMany(r => r.Observations)
                .HasForeignKey(o => o.RegionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.DatasetSource)
                .WithMany(s => s.Observations)
                .HasForeignKey(o => o.DatasetSourceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Prediction>(e =>
        {
            e.HasIndex(p => new { p.RegionId, p.Date });

            e.HasOne(p => p.Region)
                .WithMany(r => r.Predictions)
                .HasForeignKey(p => p.RegionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.ModelVersion)
                .WithMany(m => m.Predictions)
                .HasForeignKey(p => p.ModelVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed inicial das duas regiões do Projecto 1.
        builder.Entity<Region>().HasData(
            new Region { Id = 1, Name = "Chókwè", Province = "Gaza", Latitude = -24.5333, Longitude = 33.0167 },
            new Region { Id = 2, Name = "Massinga", Province = "Inhambane", Latitude = -23.3333, Longitude = 35.3833 }
        );
    }
}
