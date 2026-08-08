using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenPc.Domain.Entities;

namespace OpenPc.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.Slug).HasMaxLength(32);
        builder.Property(c => c.Name).HasMaxLength(64);
    }
}
