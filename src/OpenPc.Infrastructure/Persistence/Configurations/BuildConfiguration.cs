using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenPc.Domain.Entities;

namespace OpenPc.Infrastructure.Persistence.Configurations;

public sealed class BuildConfiguration : IEntityTypeConfiguration<Build>
{
    public void Configure(EntityTypeBuilder<Build> builder)
    {
        builder.ToTable("builds");
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.Property(b => b.Slug).HasMaxLength(24);
        builder.Property(b => b.Name).HasMaxLength(128);

        builder.HasMany(b => b.Items)
            .WithOne(i => i.Build)
            .HasForeignKey(i => i.BuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BuildItemConfiguration : IEntityTypeConfiguration<BuildItem>
{
    public void Configure(EntityTypeBuilder<BuildItem> builder)
    {
        builder.ToTable("build_items");
        // Um slot por categoria: PUT /items/{category} substitui a peça.
        builder.HasIndex(i => new { i.BuildId, i.CategoryId }).IsUnique();

        builder.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Listing)
            .WithMany()
            .HasForeignKey(i => i.ListingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
