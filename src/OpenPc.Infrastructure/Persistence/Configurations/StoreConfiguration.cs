using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenPc.Domain.Entities;

namespace OpenPc.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");
        builder.HasIndex(s => s.Slug).IsUnique();
        builder.Property(s => s.Slug).HasMaxLength(32);
        builder.Property(s => s.Name).HasMaxLength(64);
        builder.Property(s => s.BaseUrl).HasMaxLength(255);
    }
}
