using Microsoft.Extensions.Logging;
using OpenPc.Infrastructure.Prices;
using Quartz;

namespace OpenPc.Scraper.Jobs;

/// <summary>
/// Job diário de agregação de preços (M6): consolida o raw de price_history
/// na tabela price_daily e aplica a retenção (raw 90 dias, daily 24 meses).
/// Roda às 05:30 UTC — depois do catálogo diário (04:30) para pegar os
/// preços do dia completo.
/// </summary>
public sealed class PriceAggregationJob(PriceAggregationService aggregation, ILogger<PriceAggregationJob> logger) : IJob
{
    public const string Cron = "0 30 5 * * ?";

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var (upserted, rawDeleted, dailyDeleted) = await aggregation.RunAsync(
                days: 7, ct: context.CancellationToken);
            logger.LogInformation(
                "price_daily: {Upserted} linhas, {RawDeleted} raw removidos, {DailyDeleted} daily removidos",
                upserted, rawDeleted, dailyDeleted);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // shutdown em andamento
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na agregação diária de preços");
        }
    }
}
