namespace OpenPc.Domain.Prices;

/// <summary>
/// Cálculos de insight de preço para a página de ofertas (M6):
/// queda percentual em janelas (24h/7d), badge "menor preço em X dias" e
/// detecção de anomalia (queda &gt;15% vs mediana de 30 dias).
/// Domínio puro, sem I/O — testável com séries sintéticas.
/// </summary>
public static class PriceInsights
{
    /// <summary>Limiar de anomalia: preço atual &lt; 85% da mediana de 30 dias.</summary>
    public const decimal AnomalyThreshold = 0.85m;

    /// <summary>Série de preço de um produto (uma entrada por dia, price_daily).</summary>
    public readonly record struct DailyPrice(DateTime Date, decimal Price);

    public sealed record OfferInsight(
        decimal CurrentPrice,
        decimal? Price24hAgo,
        decimal? Price7dAgo,
        decimal? DropPercent24h,
        decimal? DropPercent7d,
        int LowestInDays,          // 0 = preço atual NÃO é o menor; 1..N = menor há N dias
        bool IsAnomaly);           // queda >15% vs mediana 30d

    /// <summary>
    /// Calcula os insights de um produto a partir da série diária (ascendente
    /// por data). Séries sem preço atual retornam null.
    /// Queda de janela = comparação com o preço no INÍCIO da janela (primeiro
    /// ponto com data &gt;= corte) — "preço de 7 dias atrás", não o mais recente.
    /// </summary>
    public static OfferInsight? Evaluate(IReadOnlyList<DailyPrice> series)
    {
        if (series.Count == 0)
            return null;

        var current = series[^1];
        var cutoff24h = current.Date.AddDays(-1);
        var cutoff7d = current.Date.AddDays(-7);
        var cutoff30d = current.Date.AddDays(-30);

        // primeiro ponto com data >= corte (início da janela), excluindo hoje
        decimal? AtWindowStart(DateTime cutoff)
        {
            for (var i = 0; i < series.Count - 1; i++)
                if (series[i].Date >= cutoff)
                    return series[i].Price;
            return null;
        }

        var price24h = AtWindowStart(cutoff24h);
        var price7d = AtWindowStart(cutoff7d);

        decimal? Drop(decimal? prev) =>
            prev is { } p && p > 0 ? (p - current.Price) / p : null;

        // badge "menor preço em X dias": maior janela (1..N) em que o preço
        // atual é o menor. 0 = não é o menor (o dia anterior foi menor).
        var lowestInDays = 0;
        for (var i = series.Count - 2; i >= 0; i--)
        {
            var ago = (current.Date - series[i].Date).Days;
            if (series[i].Price < current.Price)
                break;
            lowestInDays = Math.Max(lowestInDays, ago + 1);
        }

        // anomalia: queda >15% vs mediana dos últimos 30 dias
        var window30 = series.Where(s => s.Date >= cutoff30d && s.Date <= current.Date)
            .Select(s => s.Price)
            .OrderBy(p => p)
            .ToArray();
        var isAnomaly = false;
        if (window30.Length >= 3)
        {
            var median = Median(window30);
            isAnomaly = median > 0 && current.Price < median * AnomalyThreshold;
        }

        return new OfferInsight(
            CurrentPrice: current.Price,
            Price24hAgo: price24h,
            Price7dAgo: price7d,
            DropPercent24h: Drop(price24h),
            DropPercent7d: Drop(price7d),
            LowestInDays: lowestInDays,
            IsAnomaly: isAnomaly);
    }

    private static decimal Median(IReadOnlyList<decimal> sorted)
    {
        var n = sorted.Count;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2;
    }
}
