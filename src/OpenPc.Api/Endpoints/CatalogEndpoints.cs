using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
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
        products.MapGet("/health/scrapers", GetScraperHealthAsync);
    }

    /// <summary>
    /// GET /api/v1/products?category=&amp;q=&amp;brand=&amp;minPrice=&amp;maxPrice=
    /// &amp;attrs[socket]=am5&amp;sort=price_asc&amp;limit=&amp;offset=
    /// </summary>
    private static async Task<IResult> ListProductsAsync(
        AppDbContext db,
        IDistributedCache cache,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? category,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? q,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? brand,
        [Microsoft.AspNetCore.Mvc.FromQuery] decimal? minPrice,
        [Microsoft.AspNetCore.Mvc.FromQuery] decimal? maxPrice,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? sort,
        [Microsoft.AspNetCore.Mvc.FromQuery] int? limit,
        [Microsoft.AspNetCore.Mvc.FromQuery] int? offset,
        HttpContext http,
        CancellationToken ct)
    {
        var safeLimit = Math.Clamp(limit ?? 50, 1, 100);
        var safeOffset = Math.Max(offset ?? 0, 0);

        // filtros de atributo: ?attrs[socket]=am5&attrs[memory_type]=ddr5
        var attrs = new Dictionary<string, string>();
        foreach (var key in http.Request.Query.Keys)
        {
            var m = System.Text.RegularExpressions.Regex.Match(key, @"^attrs\[([^\]]+)\]$");
            if (m.Success && http.Request.Query[key].Count > 0)
                attrs[m.Groups[1].Value] = http.Request.Query[key][0]!;
        }

        var cacheKey = $"products|{category}|{q}|{brand}|{minPrice}|{maxPrice}|{sort}|{safeLimit}|{safeOffset}";
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
                query = query.Where(p => p.Attributes.Any(a => a.Key == key && a.ValueText == value));

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

        var json = JsonSerializer.Serialize(new { items, total = await projected.CountAsync(ct) }, Json);
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
}
