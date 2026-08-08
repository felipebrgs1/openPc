using Microsoft.EntityFrameworkCore;
using OpenPc.Domain.Entities;

namespace OpenPc.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();
    public DbSet<ScrapeJob> ScrapeJobs => Set<ScrapeJob>();
    public DbSet<ScrapeRun> ScrapeRuns => Set<ScrapeRun>();
    public DbSet<ProductMatchCandidate> MatchCandidates => Set<ProductMatchCandidate>();
    public DbSet<Build> Builds => Set<Build>();
    public DbSet<BuildItem> BuildItems => Set<BuildItem>();
    public DbSet<PriceDaily> PriceDaily => Set<PriceDaily>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();
    public DbSet<PriceAlertEvent> PriceAlertEvents => Set<PriceAlertEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
