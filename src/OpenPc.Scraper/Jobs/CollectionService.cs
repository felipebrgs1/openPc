using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;
using OpenPc.Scraper.Collectors;
using OpenPc.Scraper.Ingest;

namespace OpenPc.Scraper.Jobs;

/// <summary>Executa um ScrapeJob: coleta → ingestão → registro do ScrapeRun.</summary>
public sealed class CollectionService(
    AppDbContext db,
    IEnumerable<IStoreCollector> collectors,
    IngestionService ingestion,
    ScrapeAlertService alert,
    ILogger<CollectionService> logger)
{
    public async Task RunJobAsync(Guid jobId, CancellationToken ct)
    {
        var job = await db.ScrapeJobs
            .Include(j => j.Store).Include(j => j.Category)
            .SingleAsync(j => j.Id == jobId, ct);

        var collector = collectors.SingleOrDefault(c => c.StoreSlug == job.Store.Slug)
            ?? throw new NotSupportedException($"Sem collector para a loja '{job.Store.Slug}'");

        var run = new ScrapeRun
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            Status = "running",
            StartedAt = DateTime.UtcNow,
        };
        db.ScrapeRuns.Add(run);
        await db.SaveChangesAsync(ct);

        var sw = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("Coleta {Store}/{Category} iniciada", job.Store.Slug, job.Category.Slug);
            var items = await collector.CollectAsync(job.Category.Slug, ct);
            var result = await ingestion.IngestAsync(job.Store, job.Category.Slug, items, ct);

            run.Status = "ok";
            run.ItemsFound = result.ItemsFound;
            run.ItemsNew = result.NewProducts;
            run.DurationMs = sw.ElapsedMilliseconds;
            run.FinishedAt = DateTime.UtcNow;
            logger.LogInformation("Coleta {Store}/{Category} ok em {Ms} ms ({Found} itens)",
                job.Store.Slug, job.Category.Slug, sw.ElapsedMilliseconds, result.ItemsFound);
        }
        catch (Exception ex)
        {
            run.Status = "failed";
            run.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            run.DurationMs = sw.ElapsedMilliseconds;
            run.FinishedAt = DateTime.UtcNow;
            logger.LogError(ex, "Coleta {Store}/{Category} falhou", job.Store.Slug, job.Category.Slug);
        }

        await db.SaveChangesAsync(ct);

        if (run.Status == "failed")
            await alert.SendRunFailedAsync(run, job, ct);
    }

    /// <summary>Executa imediatamente os jobs habilitados (run-once).</summary>
    /// <param name="storeSlug">Filtro opcional por loja (ex: "kabum").</param>
    /// <param name="categorySlug">Filtro opcional por categoria (ex: "cpu").</param>
    public async Task RunAllEnabledAsync(string? storeSlug = null, string? categorySlug = null, CancellationToken ct = default)
    {
        var query = db.ScrapeJobs.Where(j => j.Enabled);
        if (storeSlug is not null)
            query = query.Where(j => j.Store.Slug == storeSlug);
        if (categorySlug is not null)
            query = query.Where(j => j.Category.Slug == categorySlug);

        var jobIds = await query.Select(j => j.Id).ToListAsync(ct);
        logger.LogInformation("run-once: {Count} jobs habilitados{Store}{Category}",
            jobIds.Count,
            storeSlug is null ? "" : $" (loja={storeSlug})",
            categorySlug is null ? "" : $" (categoria={categorySlug})");
        foreach (var id in jobIds)
            await RunJobAsync(id, ct);
    }
}
