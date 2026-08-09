using System.Security.Cryptography;
using System.Text;

namespace OpenPc.Scraper.Ingest;

/// <summary>
/// Chave do objeto no bucket MinIO e URL pública correspondente.
/// Chave = hash da URL de origem (dedup: a mesma foto de CDN usada por vários
/// produtos vira um único objeto) + extensão real da URL. Estável e idempotente.
/// </summary>
public static class ImageKeys
{
    public static string KeyFor(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(hash)[..40].ToLowerInvariant() + ExtForUrl(url);
    }

    /// <summary>
    /// URL pública servida pelo próprio domínio (Caddy → MinIO). Caminho
    /// relativo: o browser resolve contra a origem do site.
    /// </summary>
    public static string PublicUrl(string? publicPath, string key) =>
        string.IsNullOrWhiteSpace(publicPath) ? $"/images/{key}" : $"{publicPath.TrimEnd('/')}/{key}";

    private static string ExtForUrl(string url)
    {
        var path = url.Split('?', '#')[0];
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".avif" ? ext : ".img";
    }
}
