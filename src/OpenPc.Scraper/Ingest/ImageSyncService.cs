using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using OpenPc.Domain.Entities;
using OpenPc.Infrastructure.Persistence;

namespace OpenPc.Scraper.Ingest;

/// <summary>
/// Baixa as fotos dos CDNs das lojas e sobe para o bucket MinIO (S3) — o
/// hotlink de CDN alheio é frágil (Terabyte devolve 403 de forma
/// inconsistente; URL rot quando a loja muda o slug; ToS cinza). O ImageUrl
/// vira caminho próprio (/images/&lt;key&gt;) servido pelo Caddy no mesmo domínio.
///
/// Idempotente: chave = hash da URL de origem; re-rodar não re-baixa. Falha
/// de um item não aborta o lote (mantém a URL externa até a próxima rodada).
/// </summary>
public sealed class ImageSyncService(
    AppDbContext db,
    IHttpClientFactory http,
    IMinioClient? minio,
    IConfiguration config,
    ILogger<ImageSyncService> logger)
{
    private const int MaxParallel = 8;

    public async Task<int> SyncAsync(CancellationToken ct)
    {
        var bucket = config["Minio:Bucket"];
        if (minio is null || string.IsNullOrWhiteSpace(bucket))
        {
            logger.LogWarning("MinIO não configurado (Minio:Endpoint/Bucket ausentes) — sync de imagens pulado.");
            return 0;
        }

        var products = await db.Products
            .Where(p => p.ImageUrl != null && p.ImageUrl.StartsWith("http"))
            .ToListAsync(ct);
        if (products.Count == 0)
            return 0;

        logger.LogInformation("sync-images: {Count} produtos com imagem externa", products.Count);

        if (!await BucketExistsAsync(bucket, ct))
        {
            logger.LogWarning("sync-images: bucket '{Bucket}' não existe — crie via minio-init (compose).", bucket);
            return 0;
        }

        var done = 0;
        using var sem = new SemaphoreSlim(MaxParallel);
        var tasks = products.Select(async p =>
        {
            await sem.WaitAsync(ct);
            try
            {
                if (await SyncOneAsync(p, bucket, ct))
                    Interlocked.Increment(ref done);
            }
            finally
            {
                sem.Release();
            }
        });
        await Task.WhenAll(tasks);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("sync-images: {Done}/{Total} fotos sincronizadas", done, products.Count);
        return done;
    }

    private async Task<bool> SyncOneAsync(Product p, string bucket, CancellationToken ct)
    {
        var url = p.ImageUrl!;
        try
        {
            var key = ImageKeys.KeyFor(url);
            if (await ObjectExistsAsync(bucket, key, ct))
            {
                // outro produto já subiu a mesma foto — só aponta o caminho
                p.ImageUrl = ImageKeys.PublicUrl(config["Minio:PublicPath"], key);
                return true;
            }

            using var resp = await http.CreateClient("images").GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("sync-images: GET {Url} → {(int)resp.StatusCode}", url, resp.StatusCode);
                return false;
            }

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

            await minio!.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(key)
                .WithStreamData(new MemoryStream(bytes))
                .WithObjectSize(bytes.Length)
                .WithContentType(contentType), ct);

            p.ImageUrl = ImageKeys.PublicUrl(config["Minio:PublicPath"], key);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "sync-images: falha ao sincronizar {Url}", url);
            return false;
        }
    }

    private async Task<bool> BucketExistsAsync(string bucket, CancellationToken ct)
    {
        try
        {
            return await minio!.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "sync-images: erro ao checar bucket {Bucket}", bucket);
            return false;
        }
    }

    private async Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct)
    {
        try
        {
            await minio!.StatObjectAsync(new StatObjectArgs().WithBucket(bucket).WithObject(key), ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
