using Microsoft.EntityFrameworkCore;
using OpenPc.Domain.Compatibility;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;

namespace OpenPc.Api.Endpoints;

/// <summary>
/// Endpoints de builds (docs/specs.md §6): CRUD de build anônimo + avaliação
/// da engine de compatibilidade a cada mutação.
/// </summary>
public static class BuildEndpoints
{
    public static void MapBuildEndpoints(this WebApplication app)
    {
        var builds = app.MapGroup("/api/v1/builds");
        builds.MapPost("", CreateBuildAsync);
        builds.MapGet("/{slug}", GetBuildAsync);
        builds.MapPut("/{slug}/items/{category}", SetItemAsync);
        builds.MapDelete("/{slug}/items/{category}", RemoveItemAsync);
        builds.MapGet("/{slug}/compatibility", GetCompatibilityAsync);
        builds.MapGet("/{slug}/price-comparison", GetPriceComparisonAsync);
    }

    private static async Task<IResult> CreateBuildAsync(
        AppDbContext db,
        [Microsoft.AspNetCore.Mvc.FromBody] CreateBuildRequest? request,
        CancellationToken ct)
    {
        var build = new Build
        {
            Id = Guid.NewGuid(),
            Slug = await NewSlugAsync(db, ct),
            Name = string.IsNullOrWhiteSpace(request?.Name) ? "Meu build" : request.Name.Trim(),
            IsPublic = request?.IsPublic ?? false,
        };
        db.Builds.Add(build);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/builds/{build.Slug}", new { build.Slug, build.Name });
    }

    private static async Task<IResult> GetBuildAsync(
        string slug, AppDbContext db, CompatibilityEngine engine, CancellationToken ct)
    {
        var build = await BuildSnapshotFactory.LoadBySlugAsync(db, slug, ct);
        if (build is null)
            return Results.NotFound();

        return Results.Ok(await BuildResponseAsync(build, db, engine, ct));
    }

    private static async Task<IResult> SetItemAsync(
        string slug, string category, AppDbContext db, CompatibilityEngine engine,
        [Microsoft.AspNetCore.Mvc.FromBody] SetItemRequest request, CancellationToken ct)
    {
        var build = await BuildSnapshotFactory.LoadBySlugAsync(db, slug, ct);
        if (build is null)
            return Results.NotFound();

        var categoryEntity = await db.Categories.FirstOrDefaultAsync(c => c.Slug == category, ct);
        if (categoryEntity is null)
            return Results.BadRequest($"Categoria '{category}' desconhecida.");

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        if (product is null)
            return Results.NotFound("Produto não encontrado.");
        if (product.CategoryId != categoryEntity.Id)
            return Results.BadRequest($"Produto não pertence à categoria '{category}'.");

        Listing? listing = null;
        if (request.ListingId is not null)
        {
            listing = await db.Listings.FirstOrDefaultAsync(
                l => l.Id == request.ListingId && l.ProductId == product.Id, ct);
            if (listing is null)
                return Results.BadRequest("Listing não pertence ao produto informado.");
        }

        // build vem de AsNoTracking (snapshot) — o item precisa ser tracked
        // para a mutação persistir (bug: item existente era detached e o
        // SaveChanges silenciosamente não gravava a troca).
        var item = await db.BuildItems.FirstOrDefaultAsync(
            i => i.BuildId == build.Id && i.CategoryId == categoryEntity.Id, ct);
        if (item is null)
        {
            item = new BuildItem { Id = Guid.NewGuid(), BuildId = build.Id, CategoryId = categoryEntity.Id };
            db.BuildItems.Add(item);
        }

        item.ProductId = product.Id;
        item.ListingId = listing?.Id;
        build.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Recarrega para expor navegações (Product, Listing, Category) no response.
        build = await BuildSnapshotFactory.LoadBySlugAsync(db, slug, ct);
        return Results.Ok(await BuildResponseAsync(build!, db, engine, ct));
    }

    private static async Task<IResult> RemoveItemAsync(
        string slug, string category, AppDbContext db, CompatibilityEngine engine, CancellationToken ct)
    {
        var build = await BuildSnapshotFactory.LoadBySlugAsync(db, slug, ct);
        if (build is null)
            return Results.NotFound();

        var item = await db.BuildItems.FirstOrDefaultAsync(
            i => i.BuildId == build.Id && i.Category.Slug == category, ct);
        if (item is not null)
        {
            db.BuildItems.Remove(item);
            build.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        build = await BuildSnapshotFactory.LoadBySlugAsync(db, slug, ct);
        return Results.Ok(await BuildResponseAsync(build!, db, engine, ct));
    }

    private static async Task<IResult> GetCompatibilityAsync(
        string slug, AppDbContext db, CompatibilityEngine engine, CancellationToken ct)
    {
        var build = await BuildSnapshotFactory.LoadBySlugAsync(db, slug, ct);
        if (build is null)
            return Results.NotFound();

        var snapshot = BuildSnapshotFactory.FromBuild(build);
        var evaluation = engine.Evaluate(snapshot);
        var wattage = WattageEstimator.Estimate(snapshot);

        return Results.Ok(new
        {
            slug = build.Slug,
            name = build.Name,
            filledSlots = build.Items.Count(i => i.ProductId is not null),
            wattage = new WattageDto(wattage.BaseW, wattage.RecommendedW),
            compatibility = ToDto(evaluation),
        });
    }

    /// <summary>
    /// Otimização de compra (specs.md §6): total por loja (peças que cada loja
    /// tem em estoque) vs menor preço individual por peça.
    /// </summary>
    private static async Task<IResult> GetPriceComparisonAsync(
        string slug, AppDbContext db, CancellationToken ct)
    {
        var build = await BuildSnapshotFactory.LoadBySlugAsync(db, slug, ct);
        if (build is null)
            return Results.NotFound();

        var items = build.Items
            .Where(i => i.ProductId is not null)
            .OrderBy(i => i.Category.DisplayOrder)
            .ToArray();
        var productIds = items.Select(i => i.ProductId!.Value).Distinct().ToArray();

        var prices = productIds.Length == 0
            ? []
            : await db.Listings.AsNoTracking()
                .Where(l => productIds.Contains(l.ProductId) && l.InStock && l.PriceCash != null && l.Store.IsActive)
                .Select(l => new { l.ProductId, StoreSlug = l.Store.Slug, StoreName = l.Store.Name, l.PriceCash })
                .GroupBy(l => new { l.ProductId, l.StoreSlug, l.StoreName })
                .Select(g => new { g.Key.ProductId, g.Key.StoreSlug, g.Key.StoreName, Price = g.Min(x => x.PriceCash) })
                .ToListAsync(ct);

        var byStore = prices
            .GroupBy(p => p.StoreSlug)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                StoreSlug = g.Key,
                StoreName = g.First().StoreName,
                Total = g.Sum(p => p.Price),
                CoveredItems = g.Select(p => p.ProductId).Distinct().Count(),
                TotalItems = items.Length,
            })
            .ToArray();

        // menor preço individual por peça (mesma regra do totalPrice do build)
        var best = items.Select(i =>
        {
            decimal? price = null;
            string? store = null;
            if (i.ListingId is not null && i.Listing is not null && i.Listing.InStock)
            {
                price = i.Listing.PriceCash;
                store = i.Listing.Store?.Slug;
            }
            else
            {
                var match = prices
                    .Where(p => p.ProductId == i.ProductId)
                    .OrderBy(p => p.Price)
                    .FirstOrDefault();
                if (match is not null)
                {
                    price = match.Price;
                    store = match.StoreSlug;
                }
            }

            return new
            {
                Category = i.Category.Slug,
                i.ProductId,
                StoreSlug = store,
                Price = price,
            };
        }).ToArray();

        return Results.Ok(new
        {
            perStore = byStore,
            bestIndividual = new
            {
                total = best.Where(b => b.Price is not null).Sum(b => b.Price!.Value),
                items = best,
            },
        });
    }

    private static async Task<BuildResponseDto> BuildResponseAsync(
        Build build, AppDbContext db, CompatibilityEngine engine, CancellationToken ct)
    {
        var snapshot = BuildSnapshotFactory.FromBuild(build);
        var evaluation = engine.Evaluate(snapshot);
        var wattage = WattageEstimator.Estimate(snapshot);

        var productIds = build.Items
            .Where(i => i.ProductId is not null)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToArray();
        var bestPrices = productIds.Length == 0
            ? new Dictionary<Guid, decimal>()
            : await db.Listings.AsNoTracking()
                .Where(l => productIds.Contains(l.ProductId) && l.InStock && l.PriceCash != null)
                .GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, Price = g.Min(l => l.PriceCash) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Price!.Value, ct);

        var items = new List<BuildItemDto>();
        decimal? total = 0m;
        foreach (var item in build.Items.OrderBy(i => i.Category.DisplayOrder))
        {
            decimal? price;
            string? storeSlug = null;
            if (item.ListingId is not null && item.Listing is not null)
            {
                price = item.Listing.PriceCash;
                storeSlug = item.Listing.Store?.Slug;
            }
            else if (item.ProductId is not null)
            {
                price = bestPrices.GetValueOrDefault(item.ProductId.Value);
            }
            else
            {
                price = null;
            }

            if (price is not null)
                total += price;

            items.Add(new BuildItemDto(
                item.Category.Slug,
                item.ProductId,
                item.Product?.Name,
                item.Product?.Brand,
                item.Product?.Model,
                item.Product?.ImageUrl,
                storeSlug,
                price));
        }

        return new BuildResponseDto(
            build.Slug,
            build.Name,
            build.IsPublic,
            build.CreatedAt,
            build.UpdatedAt,
            items,
            total,
            new WattageDto(wattage.BaseW, wattage.RecommendedW),
            ToDto(evaluation));
    }

    private static CompatibilityDto ToDto(CompatibilityEvaluation evaluation) => new(
        evaluation.Errors.Select(Map).ToArray(),
        evaluation.Warnings.Select(Map).ToArray(),
        evaluation.Infos.Select(Map).ToArray());

    private static CompatibilityIssueDto Map(CompatibilityResult r) =>
        new(r.Code, r.MessagePtBr, r.InvolvedProductIds);

    private static async Task<string> NewSlugAsync(AppDbContext db, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var slug = BuildSlug.New();
            if (!await db.Builds.AnyAsync(b => b.Slug == slug, ct))
                return slug;
        }
        throw new InvalidOperationException("Não foi possível gerar um slug único de build.");
    }

    internal sealed record CreateBuildRequest(string? Name, bool? IsPublic);
    internal sealed record SetItemRequest(Guid ProductId, Guid? ListingId);
    internal sealed record BuildItemDto(
        string Category,
        Guid? ProductId,
        string? Name,
        string? Brand,
        string? Model,
        string? ImageUrl,
        string? StoreSlug,
        decimal? Price);
    internal sealed record BuildResponseDto(
        string Slug,
        string Name,
        bool IsPublic,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<BuildItemDto> Items,
        decimal? TotalPrice,
        WattageDto Wattage,
        CompatibilityDto Compatibility);
    internal sealed record CompatibilityDto(
        IReadOnlyList<CompatibilityIssueDto> Errors,
        IReadOnlyList<CompatibilityIssueDto> Warnings,
        IReadOnlyList<CompatibilityIssueDto> Infos);
    internal sealed record CompatibilityIssueDto(string Code, string Message, IReadOnlyList<Guid> Products);
    internal sealed record WattageDto(decimal BaseW, decimal RecommendedW);
}

/// <summary>Slug curto de build compartilhável (sem caracteres ambíguos).</summary>
internal static class BuildSlug
{
    private const string Alphabet = "abcdefghjkmnpqrstuvwxyz23456789";

    public static string New()
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        return new string(chars);
    }
}
