using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPc.Infrastructure.Persistence;

namespace OpenPc.Infrastructure.Prices;

/// <summary>
/// Agregação diária de preços (M6): consolida o raw de price_history na
/// tabela price_daily (menor preço em estoque por produto/dia) e aplica a
/// retenção — raw 90 dias, price_daily 24 meses (specs §3.2).
/// </summary>
public sealed class PriceAggregationService(AppDbContext db, ILogger<PriceAggregationService> logger)
{
    public const int RawRetentionDays = 90;
    public const int DailyRetentionMonths = 24;

    /// <summary>
    /// Recalcula o price_daily dos últimos <paramref name="days"/> dias
    /// (upsert por produto+dia) e aplica a retenção. Idempotente — pode
    /// rodar todo dia sem efeito colateral.
    /// </summary>
    public async Task<(int Upserted, int RawDeleted, int DailyDeleted)> RunAsync(
        int days = 1, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var since = today.AddDays(-(days - 1));
        var rawCutoff = today.AddDays(-RawRetentionDays);
        var dailyCutoff = today.AddMonths(-DailyRetentionMonths);

        // 1) Upsert do price_daily: menor preço em estoque por (produto, dia).
        //    Preço nulo/fora de estoque não gera linha (produto "sem preço" hoje).
        var daily = await db.PriceHistory.AsNoTracking()
            .Where(h => h.InStock
                        && h.PriceCash > 0
                        && h.CollectedAt >= since
                        && h.CollectedAt < today.AddDays(1))
            .GroupBy(h => new { h.Listing.ProductId, Date = h.CollectedAt.Date })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Date,
                MinPrice = g.Min(h => h.PriceCash),
                ListingId = g.OrderBy(h => h.PriceCash).Select(h => (Guid?)h.ListingId).First(),
            })
            .ToListAsync(ct);

        var existing = await db.PriceDaily.AsNoTracking()
            .Where(d => d.Date >= since)
            .ToDictionaryAsync(d => (d.ProductId, d.Date), ct);

        var upserted = 0;
        foreach (var row in daily)
        {
            var key = (row.ProductId, row.Date);
            if (existing.TryGetValue(key, out var current))
            {
                if (current.MinPrice != row.MinPrice || current.ListingId != row.ListingId)
                {
                    current.MinPrice = row.MinPrice;
                    current.ListingId = row.ListingId;
                    current.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                db.PriceDaily.Add(new Domain.Entities.PriceDaily
                {
                    Id = Guid.NewGuid(),
                    ProductId = row.ProductId,
                    Date = row.Date,
                    MinPrice = row.MinPrice,
                    ListingId = row.ListingId,
                });
            }
            upserted++;
        }

        // 2) Retenção: raw > 90 dias e price_daily > 24 meses.
        var rawOld = await db.PriceHistory
            .Where(h => h.CollectedAt < rawCutoff)
            .ExecuteDeleteAsync(ct);
        var dailyOld = await db.PriceDaily
            .Where(d => d.Date < dailyCutoff)
            .ExecuteDeleteAsync(ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "price_daily: {Upserted} linhas (desde {Since}), raw removidos: {RawDeleted}, daily removidos: {DailyDeleted}",
            upserted, since.ToShortDateString(), rawOld, dailyOld);

        return (upserted, rawOld, dailyOld);
    }
}
