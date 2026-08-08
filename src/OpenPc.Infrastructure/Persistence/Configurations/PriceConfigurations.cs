using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenPc.Domain.Entities;

namespace OpenPc.Infrastructure.Persistence.Configurations;

public sealed class PriceDailyConfiguration : IEntityTypeConfiguration<PriceDaily>
{
    public void Configure(EntityTypeBuilder<PriceDaily> builder)
    {
        builder.ToTable("price_daily");
        builder.HasIndex(d => new { d.ProductId, d.Date }).IsUnique();
        builder.HasIndex(d => d.Date);
        builder.Property(d => d.MinPrice).HasPrecision(12, 2);
        builder.HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("price_alerts");
        builder.HasIndex(a => a.Token).IsUnique();
        builder.HasIndex(a => new { a.ProductId, a.Email });
        builder.Property(a => a.Email).HasMaxLength(320);
        builder.Property(a => a.Token).HasMaxLength(64);
        builder.Property(a => a.TargetPrice).HasPrecision(12, 2);
        builder.HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PriceAlertEventConfiguration : IEntityTypeConfiguration<PriceAlertEvent>
{
    public void Configure(EntityTypeBuilder<PriceAlertEvent> builder)
    {
        builder.ToTable("price_alert_events");
        builder.HasIndex(e => new { e.AlertId, e.TriggeredAt });
        builder.Property(e => e.PriceAtTrigger).HasPrecision(12, 2);
        builder.HasOne(e => e.Alert)
            .WithMany()
            .HasForeignKey(e => e.AlertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
