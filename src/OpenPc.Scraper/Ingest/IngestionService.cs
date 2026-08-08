using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Scraper.Collectors;
using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Ingest;

/// <summary>
/// Persiste listings coletados com dedup em 3 níveis:
/// 1. part number do fabricante (match exato)
/// 2. chave determinística marca+modelo (match por tokens)
/// 3. fila de revisão (sem match confiável → produto próprio + candidato)
/// </summary>
public sealed class IngestionService(AppDbContext db, ILogger<IngestionService> logger)
{
    public async Task<IngestResult> IngestAsync(
        Store store, string categorySlug, IReadOnlyList<RawListing> items, CancellationToken ct)
    {
        var category = await db.Categories.SingleAsync(c => c.Slug == categorySlug, ct);

        // descarta ruído de categoria (contact frame em cpu, suporte em gpu,
        // fonte de notebook / cross-listing em psu...) antes de persistir.
        var relevant = items
            .Where(i => !CategoryNoiseFilter.IsNoise(categorySlug, i.Title))
            .ToArray();
        if (relevant.Length < items.Count)
            logger.LogInformation(
                "Ingestão {Store}/{Category}: {Skipped} itens descartados como ruído de categoria",
                store.Slug, categorySlug, items.Count - relevant.Length);
        items = relevant;

        var byPartNumber = await db.Products
            .Where(p => p.CategoryId == category.Id && p.PartNumber != null)
            .ToDictionaryAsync(p => p.PartNumber!, ct);

        var byMatchKey = await db.Products
            .Where(p => p.CategoryId == category.Id && p.Model != null && p.Brand != null)
            .ToDictionaryAsync(p => p.Model, ct);

        // atributos da categoria em memória — evita 1 query por item e
        // duplicatas quando vários listings casam no mesmo produto novo
        var attributesByProduct = await db.ProductAttributes
            .Where(a => a.Product.CategoryId == category.Id)
            .GroupBy(a => a.ProductId)
            .ToDictionaryAsync(g => g.Key, g => g.ToDictionary(a => a.Key), ct);

        var existingListings = await db.Listings
            .Where(l => l.StoreId == store.Id)
            .ToDictionaryAsync(l => l.StoreSku, ct);

        var newProducts = 0;
        var newListings = 0;
        var newCandidates = 0;

        // produtos cujo menor preço em estoque caiu neste run — alvo do
        // disparo de alertas de preço (M6)
        var priceDropProductIds = new HashSet<Guid>();
        var minPriceByProduct = await db.Listings.AsNoTracking()
            .Where(l => l.InStock && l.PriceCash != null)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Min = g.Min(l => l.PriceCash) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Min, ct);

        // categorias com padrão de match key conhecido: ausência de âncora
        // sinaliza problema; nas demais, produtos únicos são o caso normal.
        var expectAnchor = categorySlug is "cpu" or "gpu";

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            var product = ResolveProduct(item, category.Id, byPartNumber, byMatchKey);
            if (product is null)
            {
                product = new Product
                {
                    Id = Guid.NewGuid(),
                    CategoryId = category.Id,
                    Brand = NormalizeBrand(item.Manufacturer ?? item.Title),
                    Model = NormalizeModel(item.MatchKey),
                    Name = CleanTitle(item.Title),
                    PartNumber = item.PartNumber is null ? null : PartNumber.Normalize(item.PartNumber),
                    ImageUrl = item.Thumbnail,
                    SpecSource = "scraper",
                };
                db.Products.Add(product);
                if (product.PartNumber is not null)
                    byPartNumber[product.PartNumber] = product;
                byMatchKey[product.Model] = product;
                newProducts++;

                // sem âncora (part number E match key) em categoria que exige:
                // sinaliza para revisão manual
                if (expectAnchor && item.PartNumber is null && item.MatchKey is null)
                {
                    db.MatchCandidates.Add(new ProductMatchCandidate
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        StoreId = store.Id,
                        StoreSku = item.StoreSku,
                        Title = item.Title,
                        Reason = "no_anchor",
                    });
                    newCandidates++;
                }
            }
            else
            {
                product.Name = CleanTitle(item.Title);
                product.ImageUrl ??= item.Thumbnail;
                if (product.PartNumber is null && item.PartNumber is not null)
                    product.PartNumber = PartNumber.Normalize(item.PartNumber);
                product.UpdatedAt = DateTime.UtcNow;
            }

            // atributos (upsert via cache em memória)
            ApplyAttributes(product, item.Specs, attributesByProduct);

            var listing = existingListings.GetValueOrDefault(item.StoreSku);
            if (listing is null)
            {
                listing = new Listing
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    StoreId = store.Id,
                    StoreSku = item.StoreSku,
                    Url = item.Url,
                    Title = item.Title,
                };
                db.Listings.Add(listing);
                existingListings[item.StoreSku] = listing;
                newListings++;
            }

            listing.ProductId = product.Id;
            listing.Url = item.Url;
            listing.Title = item.Title;
            listing.PriceCash = item.PriceCash;
            listing.PriceCard = item.PriceCard;
            listing.Installments = item.Installments;
            listing.InstallmentText = item.InstallmentText;
            listing.InStock = item.InStock;
            listing.Thumbnail ??= item.Thumbnail;
            listing.LastSeenAt = DateTime.UtcNow;

            // queda de preço? registra para o disparo de alertas (M6):
            // compara com o menor preço em estoque conhecido do produto.
            if (item.InStock && item.PriceCash is { } newPrice
                && minPriceByProduct.TryGetValue(product.Id, out var prevMin)
                && newPrice < prevMin)
            {
                priceDropProductIds.Add(product.Id);
            }

            // preço mudou? append ao histórico (append-only)
            var last = await db.PriceHistory
                .Where(h => h.ListingId == listing.Id)
                .OrderByDescending(h => h.CollectedAt)
                .FirstOrDefaultAsync(ct);

            if (last is null || last.PriceCash != item.PriceCash || last.InStock != item.InStock)
            {
                db.PriceHistory.Add(new PriceHistory
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    PriceCash = item.PriceCash ?? 0,
                    PriceCard = item.PriceCard,
                    InStock = item.InStock,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ingestão {Store}/{Category}: {Items} itens, {NewProducts} produtos novos, {NewListings} listings novos, {Candidates} na fila",
            store.Slug, categorySlug, items.Count, newProducts, newListings, newCandidates);

        return new IngestResult(items.Count, newProducts, newListings, newCandidates, priceDropProductIds);
    }

    private Product? ResolveProduct(
        RawListing item, Guid categoryId,
        IDictionary<string, Product> byPartNumber,
        IDictionary<string, Product> byMatchKey)
    {
        if (item.PartNumber is not null
            && byPartNumber.TryGetValue(PartNumber.Normalize(item.PartNumber), out var byPn))
            return byPn;

        if (item.MatchKey is not null && byMatchKey.TryGetValue(item.MatchKey, out var byKey))
            return byKey;

        return null; // sem match: criar produto novo
    }

    private void ApplyAttributes(
        Product product, IReadOnlyDictionary<string, string> specs,
        IDictionary<Guid, Dictionary<string, ProductAttribute>> cache)
    {
        if (specs.Count == 0)
            return;

        if (!cache.TryGetValue(product.Id, out var existing))
            cache[product.Id] = existing = new Dictionary<string, ProductAttribute>();

        foreach (var (key, value) in specs)
        {
            if (existing.TryGetValue(key, out var attr))
            {
                if (attr.ValueText == value)
                    continue;
                attr.ValueText = value;
                attr.ValueNum = ParseNum(value);
                attr.ValueBool = ParseBool(value);
            }
            else
            {
                attr = new ProductAttribute
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Key = key,
                    ValueText = value,
                    ValueNum = ParseNum(value),
                    ValueBool = ParseBool(value),
                };
                existing[key] = attr;
                db.ProductAttributes.Add(attr);
            }
        }
    }

    private static decimal? ParseNum(string value) =>
        decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;

    private static bool? ParseBool(string value) =>
        bool.TryParse(value, out var b) ? b : null;

    private static string NormalizeBrand(string title)
    {
        var t = MatchKey.Normalize(title);
        foreach (var brand in new[] { "amd", "intel", "nvidia", "gigabyte", "asus", "msi", "kingston", "corsair", "samsung", "wd", "seagate", "xpg", "teamgroup", "coolermaster", "deepcool", "corsair" })
        {
            if (t.Contains(brand, StringComparison.Ordinal))
                return brand switch
                {
                    "wd" => "Western Digital",
                    "teamgroup" => "TeamGroup",
                    "coolermaster" => "Cooler Master",
                    _ => brand,
                };
        }
        return "Outros";
    }

    private static string NormalizeModel(string? matchKey) =>
        string.IsNullOrWhiteSpace(matchKey) ? Guid.NewGuid().ToString("N")[..12] : matchKey;

    /// <summary>Nome curto para exibição: remove prefixos genéricos da loja.</summary>
    private static string CleanTitle(string title)
    {
        var t = title.Trim();
        foreach (var prefix in new[] { "Processador ", "Placa de vídeo ", "Memória ", "Gabinete " })
        {
            if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                t = t[prefix.Length..];
                break;
            }
        }
        return t;
    }
}

public sealed record IngestResult(
    int ItemsFound,
    int NewProducts,
    int NewListings,
    int NewCandidates,
    IReadOnlyCollection<Guid> PriceDropProductIds);
