using Microsoft.EntityFrameworkCore;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Context;

public class MultiGuideDbContext : DbContext
{
    public MultiGuideDbContext(DbContextOptions<MultiGuideDbContext> options)
        : base(options)
    {
    }

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<TourArea> TourAreas => Set<TourArea>();
    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AccessPass> AccessPasses => Set<AccessPass>();
    public DbSet<PlayLog> PlayLogs => Set<PlayLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MultiGuideDbContext).Assembly);
    }
}
