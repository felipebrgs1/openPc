using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ScraperWorker>();

var host = builder.Build();
await host.RunAsync();

/// <summary>
/// Worker base do OpenPc.Scraper.
/// Fronteira M0: nenhum coletor de loja existe ainda — a estratégia por loja
/// é definida no M1 (spike de scraping), e o pipeline completo chega no M2.
/// Este worker só mantém o processo vivo e loga o estado.
/// </summary>
public sealed class ScraperWorker(ILogger<ScraperWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "OpenPc.Scraper iniciado (M0). Coletores de loja chegam no M1 (spike) / M2 (pipeline).");

        // Aguarda indefinidamente até o host ser parado.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
