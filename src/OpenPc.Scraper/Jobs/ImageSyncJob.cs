using OpenPc.Scraper.Ingest;
using Quartz;

namespace OpenPc.Scraper.Jobs;

/// <summary>
/// Sincroniza as fotos dos CDNs para o bucket MinIO (mesma lógica do comando
/// manual `sync-images`). Roda depois da coleta diária do catálogo (04:30),
/// pegando as URLs novas do dia.
/// </summary>
public sealed class ImageSyncJob(ImageSyncService sync) : IJob
{
    public const string Cron = "0 30 5 * * ?"; // 05:30 diário

    public async Task Execute(IJobExecutionContext context)
        => await sync.SyncAsync(context.CancellationToken);
}
