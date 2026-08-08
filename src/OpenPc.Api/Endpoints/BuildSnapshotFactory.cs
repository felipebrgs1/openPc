using Microsoft.EntityFrameworkCore;
using OpenPc.Domain.Compatibility;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;

namespace OpenPc.Api.Endpoints;

/// <summary>Constrói BuildSnapshot (engine pura, sem I/O) a partir de entidades EF.</summary>
public static class BuildSnapshotFactory
{
    public static async Task<Build?> LoadBySlugAsync(AppDbContext db, string slug, CancellationToken ct) =>
        await db.Builds.AsNoTracking()
            .Include(b => b.Items).ThenInclude(i => i.Category)
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Attributes)
            .Include(b => b.Items).ThenInclude(i => i.Listing).ThenInclude(l => l!.Store)
            .FirstOrDefaultAsync(b => b.Slug == slug, ct);

    public static BuildSnapshot FromBuild(Build build)
    {
        var parts = new List<PartSpec>();
        foreach (var item in build.Items.Where(i => i.Product is not null))
        {
            var part = ToPartSpec(item.Product!, item.Category.Slug, item.Product!.Attributes);
            if (part is not null)
                parts.Add(part);
        }

        return new BuildSnapshot
        {
            BuildId = build.Id,
            Slug = build.Slug,
            Parts = parts,
        };
    }

    /// <summary>Mapeia Product + attributes EAV → PartSpec. null = categoria fora do contrato.</summary>
    public static PartSpec? ToPartSpec(
        Product product, string categorySlug, IEnumerable<ProductAttribute> attributes)
    {
        var category = PartCategorySlugs.FromSlug(categorySlug);
        if (category is null)
            return null;

        var dict = attributes.ToDictionary(
            a => a.Key,
            a => new AttrValue(a.ValueText, a.ValueNum, a.ValueBool),
            StringComparer.OrdinalIgnoreCase);

        return new PartSpec
        {
            ProductId = product.Id,
            Category = category.Value,
            Brand = product.Brand,
            Model = product.Model,
            Name = product.Name,
            Attributes = dict,
        };
    }
}
