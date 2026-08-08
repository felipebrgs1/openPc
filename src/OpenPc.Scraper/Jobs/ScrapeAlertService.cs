using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenPc.Domain.Entities;

namespace OpenPc.Scraper.Jobs;

/// <summary>
/// Alerta simples de scraper quebrado: quando um ScrapeRun termina como
/// `failed`, envia um POST JSON para o webhook configurado (Alerts:WebhookUrl).
/// Fire-and-forget com timeout curto — o alerta nunca pode derrubar o job.
/// Sem URL configurada, apenas loga (modo silencioso).
/// </summary>
public sealed class ScrapeAlertService(
    HttpClient http,
    IConfiguration config,
    ILogger<ScrapeAlertService> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task SendRunFailedAsync(ScrapeRun run, ScrapeJob job, CancellationToken ct)
    {
        var url = config["Alerts:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning(
                "Scrape falhou: {Store}/{Category} — {Error} (sem webhook configurado em Alerts:WebhookUrl)",
                job.Store.Slug, job.Category.Slug, run.Error);
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event = "scrape_run_failed",
            store = job.Store.Slug,
            category = job.Category.Slug,
            status = run.Status,
            error = run.Error,
            startedAt = run.StartedAt,
            finishedAt = run.FinishedAt,
            durationMs = run.DurationMs,
        }, Json);

        try
        {
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var response = await http.PostAsync(url, content, cts.Token);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Alerta de scrape: webhook respondeu {Status} ({Store}/{Category})",
                    (int)response.StatusCode, job.Store.Slug, job.Category.Slug);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Alerta de scrape falhou ao enviar webhook ({Store}/{Category})",
                job.Store.Slug, job.Category.Slug);
        }
    }
}
