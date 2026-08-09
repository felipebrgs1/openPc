using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OpenPc.Domain.Compatibility;
using OpenPc.Infrastructure.Persistence;

namespace OpenPc.Api.Endpoints;

public static class CatalogEndpoints
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var products = app.MapGroup("/api/v1");

        products.MapGet("/products", ListProductsAsync);
        products.MapGet("/products/{id:guid}", GetProductAsync);
        products.MapGet("/products/{id:guid}/prices", GetProductPricesAsync);
        products.MapGet("/health/scrapers", GetScraperHealthAsync);
    }

    /// <summary>
    /// GET /api/v1/products?category=&amp;q=&amp;brand=&amp;minPrice=&amp;maxPrice=
    /// &amp;attrs[socket]=am5&amp;compatibleWith=&lt;buildSlug&gt;&amp;showIncompatible=true
    /// &amp;sort=price_asc&amp;limit=&amp;offset=
    /// </summary>
    private static async Task<IResult> ListProductsAsync(
        AppDbContext db,
        IDistributedCache cache,
        CompatibilityEngine engine,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? category,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? q,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? brand,
        [Microsoft.AspNetCore.Mvc.FromQuery] decimal? minPrice,
        [Microsoft.AspNetCore.Mvc.FromQuery] decimal? maxPrice,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? sort,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? compatibleWith,
        [Microsoft.AspNetCore.Mvc.FromQuery] int? limit,
        [Microsoft.AspNetCore.Mvc.FromQuery] int? offset,
        HttpContext http,
        CancellationToken ct,
        [Microsoft.AspNetCore.Mvc.FromQuery] bool showIncompatible = false)
    {
        var safeLimit = Math.Clamp(limit ?? 50, 1, 5000);
        var safeOffset = Math.Max(offset ?? 0, 0);

        // filtros de atributo: ?attrs[socket]=am5&attrs[memory_type]=ddr5
        var attrs = new Dictionary<string, string>();
        foreach (var key in http.Request.Query.Keys)
        {
            var m = System.Text.RegularExpressions.Regex.Match(key, @"^attrs\[([^\]]+)\]$");
            if (m.Success && http.Request.Query[key].Count > 0)
                attrs[m.Groups[1].Value] = http.Request.Query[key][0]!;
        }

        // filtro da engine: só produtos que não geram erro novo no build;
        // com showIncompatible=true, inclui todos e anexa os motivos (blockedBy).
        HashSet<Guid>? compatibleIds = null;
        Dictionary<Guid, BlockedByDto[]>? blockedBy = null;
        if (!string.IsNullOrWhiteSpace(compatibleWith))
        {
            if (category is null)
                return Results.BadRequest("O filtro compatibleWith exige o parâmetro category.");
            var result = await EvaluateCompatibilityAsync(db, engine, compatibleWith, category, showIncompatible, ct);
            if (result is null)
                return Results.NotFound($"Build '{compatibleWith}' não encontrado.");
            (compatibleIds, blockedBy) = result.Value;
        }

        var cacheKey = $"products|{category}|{q}|{brand}|{minPrice}|{maxPrice}|{sort}|{safeLimit}|{safeOffset}|{compatibleWith}|{showIncompatible}";
        if (attrs is not null)
            foreach (var (k, v) in attrs.OrderBy(a => a.Key))
                cacheKey += $"|{k}={v}";

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
            return Results.Text(cached, "application/json");

        var query = db.Products.AsNoTracking().AsQueryable();
        if (category is not null)
            query = query.Where(p => p.Category.Slug == category);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{q}%"));
        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(p => p.Brand == brand);
        if (attrs is not null)
            foreach (var (key, value) in attrs)
            {
                // comparação tolerante a espaço: "LGA 1700", "lga 1700" e
                // "lga1700" (variações de título) filtram o mesmo conjunto.
                // Só funções traduzíveis (string.Replace → SQL REPLACE).
                var normalized = value.Replace(" ", "");
                query = query.Where(p => p.Attributes.Any(a =>
                    a.Key == key
                    && a.ValueText != null
                    && (a.ValueText == value || a.ValueText.Replace(" ", "") == normalized)));
            }

        if (compatibleIds is not null)
            query = query.Where(p => compatibleIds.Contains(p.Id));

        var projected = query.Select(p => new
        {
            p.Id,
            p.Name,
            p.Brand,
            p.Model,
            p.PartNumber,
            p.ImageUrl,
            CategorySlug = p.Category.Slug,
            Price = p.Listings
                .Where(l => l.InStock && l.PriceCash != null)
                .Min(l => (decimal?)l.PriceCash),
            StoreCount = p.Listings.Count(l => l.InStock),
        });

        if (minPrice is not null)
            projected = projected.Where(x => x.Price >= minPrice);
        if (maxPrice is not null)
            projected = projected.Where(x => x.Price <= maxPrice);

        // itens sem valor (nenhum listing em estoque com preço) não fazem parte
        // do catálogo — mesma regra do contador por loja da home.
        projected = projected.Where(x => x.Price != null);

        projected = sort switch
        {
            "price_desc" => projected.OrderByDescending(x => x.Price),
            "name" => projected.OrderBy(x => x.Name),
            _ => projected.OrderBy(x => x.Price), // price_asc (default)
        };

        var items = await projected
            .Skip(safeOffset)
            .Take(safeLimit)
            .ToListAsync(ct);
        var total = await projected.CountAsync(ct);

        object payload = blockedBy is null
            ? new { items, total }
            : new
            {
                items = items.Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Brand,
                    i.Model,
                    i.PartNumber,
                    i.ImageUrl,
                    i.CategorySlug,
                    i.Price,
                    i.StoreCount,
                    BlockedBy = blockedBy.GetValueOrDefault(i.Id) ?? [],
                }).ToList(),
                total,
            };

        var json = JsonSerializer.Serialize(payload, Json);
        await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        }, ct);

        return Results.Text(json, "application/json");
    }

    private static async Task<IResult> GetProductAsync(Guid id, AppDbContext db, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Brand,
                p.Model,
                p.PartNumber,
                p.ImageUrl,
                CategorySlug = p.Category.Slug,
                Specs = p.Attributes.Select(a => new
                {
                    a.Key,
                    a.ValueText,
                    a.ValueNum,
                    a.ValueBool,
                }),
                Listings = p.Listings.Select(l => new
                {
                    StoreSlug = l.Store.Slug,
                    StoreName = l.Store.Name,
                    l.PriceCash,
                    l.PriceCard,
                    l.Installments,
                    l.InstallmentText,
                    l.InStock,
                    l.Url,
                    l.Thumbnail,
                    l.LastSeenAt,
                }),
            })
            .FirstOrDefaultAsync(ct);

        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    /// <summary>GET /api/v1/products/{id}/prices?days=90 — série diária (menor preço do dia) para o gráfico.</summary>
    private static async Task<IResult> GetProductPricesAsync(
        Guid id, AppDbContext db,
        [Microsoft.AspNetCore.Mvc.FromQuery] int? days,
        CancellationToken ct)
    {
        var safeDays = Math.Clamp(days ?? 90, 1, 365);
        var since = DateTime.UtcNow.Date.AddDays(-safeDays);

        var series = await db.PriceHistory.AsNoTracking()
            .Where(h => h.Listing.ProductId == id && h.CollectedAt >= since)
            .GroupBy(h => h.CollectedAt.Date)
            .Select(g => new { Date = g.Key, Price = g.Min(h => h.PriceCash) })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return Results.Ok(series);
    }

    private static async Task<IResult> GetScraperHealthAsync(AppDbContext db, CancellationToken ct)
    {
        var latest = await db.ScrapeRuns.AsNoTracking()
            .GroupBy(r => r.Job.Store.Slug)
            .Select(g => new
            {
                Store = g.Key,
                LastRun = g.OrderByDescending(r => r.StartedAt).Select(r => new
                {
                    r.Status,
                    r.ItemsFound,
                    r.ItemsNew,
                    r.DurationMs,
                    r.StartedAt,
                    Category = r.Job.Category.Slug,
                }).First(),
            })
            .OrderBy(x => x.Store)
            .ToListAsync(ct);

        return Results.Ok(latest);
    }

    /// <summary>
    /// Avalia cada produto da categoria contra o build (slot hipotético).
    /// Sem showIncompatible: Compatible = ids sem erro novo. Com: Reasons =
    /// motivos (errors) por produto e Compatible = null (lista não filtrada).
    /// null = build inexistente.
    /// </summary>
    private static async Task<(HashSet<Guid>? Compatible, Dictionary<Guid, BlockedByDto[]>? Reasons)?> EvaluateCompatibilityAsync(
        AppDbContext db, CompatibilityEngine engine, string buildSlug, string categorySlug,
        bool showIncompatible, CancellationToken ct)
    {
        var build = await BuildSnapshotFactory.LoadBySlugAsync(db, buildSlug, ct);
        if (build is null)
            return null;

        var snapshot = BuildSnapshotFactory.FromBuild(build);

        var candidates = await db.Products.AsNoTracking()
            .Where(p => p.Category.Slug == categorySlug)
            .Select(p => new { p, Attributes = p.Attributes.ToArray() })
            .ToListAsync(ct);

        HashSet<Guid>? compatible = showIncompatible ? null : new HashSet<Guid>();
        Dictionary<Guid, BlockedByDto[]>? reasons = showIncompatible ? new Dictionary<Guid, BlockedByDto[]>() : null;

        foreach (var c in candidates)
        {
            var part = BuildSnapshotFactory.ToPartSpec(c.p, categorySlug, c.Attributes);
            if (part is null)
                continue;

            var evaluation = engine.Evaluate(snapshot.With(part));
            if (!evaluation.HasErrors)
                compatible?.Add(c.p.Id);
            else if (reasons is not null)
                reasons[c.p.Id] = evaluation.Errors
                    .Select(e => new BlockedByDto(e.Code, e.MessagePtBr))
                    .ToArray();
        }

        return (compatible, reasons);
    }

    internal sealed record BlockedByDto(string Code, string Message);
}
