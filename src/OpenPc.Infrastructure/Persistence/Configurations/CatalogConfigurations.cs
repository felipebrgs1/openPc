using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenPc.Domain.Entities;

namespace OpenPc.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasIndex(p => p.PartNumber);
        builder.Property(p => p.Brand).HasMaxLength(64);
        builder.Property(p => p.Model).HasMaxLength(128);
        builder.Property(p => p.Name).HasMaxLength(512);
        builder.Property(p => p.PartNumber).HasMaxLength(64);
        builder.Property(p => p.Ean).HasMaxLength(32);
        builder.Property(p => p.ImageUrl).HasMaxLength(512);
        builder.Property(p => p.SpecSource).HasMaxLength(16);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("product_attributes");
        builder.HasIndex(a => new { a.ProductId, a.Key }).IsUnique();
        builder.Property(a => a.Key).HasMaxLength(48);
        builder.Property(a => a.ValueText).HasMaxLength(256);
        builder.Property(a => a.Source).HasMaxLength(16);

        builder.HasOne(a => a.Product)
            .WithMany(p => p.Attributes)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("listings");
        builder.HasIndex(l => new { l.StoreId, l.StoreSku }).IsUnique();
        builder.Property(l => l.StoreSku).HasMaxLength(256);
        builder.Property(l => l.Url).HasMaxLength(512);
        builder.Property(l => l.Title).HasMaxLength(512);
        builder.Property(l => l.InstallmentText).HasMaxLength(64);
        builder.Property(l => l.Thumbnail).HasMaxLength(512);
        builder.Property(l => l.PriceCash).HasPrecision(12, 2);
        builder.Property(l => l.PriceCard).HasPrecision(12, 2);
        builder.Property(l => l.SpecsCollectedAt);

        builder.HasOne(l => l.Product)
            .WithMany(p => p.Listings)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Store)
            .WithMany()
            .HasForeignKey(l => l.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("price_history");
        builder.HasIndex(h => new { h.ListingId, h.CollectedAt });
        builder.Property(h => h.PriceCash).HasPrecision(12, 2);
        builder.Property(h => h.PriceCard).HasPrecision(12, 2);

        builder.HasOne(h => h.Listing)
            .WithMany(l => l.PriceHistory)
            .HasForeignKey(h => h.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ScrapeJobConfiguration : IEntityTypeConfiguration<ScrapeJob>
{
    public void Configure(EntityTypeBuilder<ScrapeJob> builder)
    {
        builder.ToTable("scrape_jobs");
        builder.Property(j => j.ScheduleCron).HasMaxLength(32);

        builder.HasOne(j => j.Store)
            .WithMany()
            .HasForeignKey(j => j.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Category)
            .WithMany()
            .HasForeignKey(j => j.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ScrapeRunConfiguration : IEntityTypeConfiguration<ScrapeRun>
{
    public void Configure(EntityTypeBuilder<ScrapeRun> builder)
    {
        builder.ToTable("scrape_runs");
        builder.HasIndex(r => new { r.JobId, r.StartedAt });
        builder.Property(r => r.Status).HasMaxLength(16);

        builder.HasOne(r => r.Job)
            .WithMany()
            .HasForeignKey(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductMatchCandidateConfiguration : IEntityTypeConfiguration<ProductMatchCandidate>
{
    public void Configure(EntityTypeBuilder<ProductMatchCandidate> builder)
    {
        builder.ToTable("product_match_candidates");
        builder.HasIndex(c => new { c.StoreId, c.StoreSku });
        builder.Property(c => c.StoreSku).HasMaxLength(256);
        builder.Property(c => c.Title).HasMaxLength(512);
        builder.Property(c => c.Reason).HasMaxLength(32);
        builder.Property(c => c.Similarity).HasPrecision(4, 3);

        builder.HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
