using Microsoft.EntityFrameworkCore;
using OpenPc.Domain.Prices;
using OpenPc.Infrastructure.Persistence;

namespace OpenPc.Api.Endpoints;

/// <summary>
/// Página de ofertas (M6): maiores quedas de preço em 24h/7 dias + badge
/// "menor preço em X dias" + flag de anomalia (&gt;15% vs mediana 30 dias).
/// Fonte: price_daily (agregação diária) — nunca o raw esparso.
/// </summary>
public static class OffersEndpoints
{
    public static void MapOffersEndpoints(this WebApplication app)
    {
        var offers = app.MapGroup("/api/v1/offers");
        offers.MapGet("", ListOffersAsync);
    }

    private static async Task<IResult> ListOffersAsync(
        AppDbContext db,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? period,   // 24h | 7d (default 7d)
        [Microsoft.AspNetCore.Mvc.FromQuery] int? limit,
        CancellationToken ct)
    {
        var safeLimit = Math.Clamp(limit ?? 30, 1, 100);
        var today = DateTime.UtcNow.Date;
        var windowDays = period == "24h" ? 2 : 8; // hoje + janela
        var since = today.AddDays(-30);           // mediana precisa de 30 dias

        // Série diária completa dos últimos 30 dias para cada produto com
        // preço em estoque hoje (price_daily alimenta a série; o preço atual
        // vem do menor listing em estoque).
        var rows = await db.PriceDaily.AsNoTracking()
            .Where(d => d.Date >= since)
            .Where(d => d.Product.Listings.Any(l => l.InStock && l.PriceCash != null))
            .Select(d => new
            {
                d.ProductId,
                d.Date,
                d.MinPrice,
                Product = new
                {
                    d.Product.Id,
                    d.Product.Name,
                    d.Product.Brand,
                    d.Product.ImageUrl,
                    Category = d.Product.Category.Slug,
                    CurrentPrice = d.Product.Listings
                        .Where(l => l.InStock && l.PriceCash != null)
                        .Min(l => (decimal?)l.PriceCash),
                },
            })
            .ToListAsync(ct);

        // Só produtos com preço atual definido e que caíram na janela
        var insights = new List<(object Product, PriceInsights.OfferInsight Insight)>();
        foreach (var group in rows.GroupBy(r => r.ProductId))
        {
            var current = group.First().Product.CurrentPrice;
            if (current is null)
                continue;

            var series = group
                .Select(g => new PriceInsights.DailyPrice(g.Date, g.MinPrice))
                .OrderBy(s => s.Date)
                .ToList();

            // garante que o preço de hoje esteja na série (price_daily pode
            // ainda não ter rodado hoje — usa o preço atual como proxy)
            if (series.Count == 0 || series[^1].Date != today)
                series.Add(new PriceInsights.DailyPrice(today, current.Value));

            var insight = PriceInsights.Evaluate(series);
            if (insight is null)
                continue;

            // filtro da janela: precisa ter queda mensurável no período
            var drop = windowDays == 2 ? insight.DropPercent24h : insight.DropPercent7d;
            if (drop is null or <= 0)
                continue;

            insights.Add((group.First().Product, insight));
        }

        var ranked = insights
            .OrderByDescending(x => windowDays == 2 ? x.Insight.DropPercent24h : x.Insight.DropPercent7d)
            .Take(safeLimit)
            .Select(x => new
            {
                Product = x.Product,
                x.Insight.CurrentPrice,
                x.Insight.Price24hAgo,
                x.Insight.Price7dAgo,
                x.Insight.DropPercent24h,
                x.Insight.DropPercent7d,
                x.Insight.LowestInDays,
                x.Insight.IsAnomaly,
            })
            .ToList();

        return Results.Ok(new { items = ranked, period = period == "24h" ? "24h" : "7d" });
    }
}
