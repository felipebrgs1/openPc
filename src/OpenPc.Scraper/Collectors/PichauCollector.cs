using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Pichau (VTEX + Cloudflare). Cards com "de R$ X por R$ Y" (PIX à vista).
/// Paginação SPA por clique em "?page=N" (validada no M1).
/// </summary>
public sealed partial class PichauCollector : BrowserCollectorBase
{
    public PichauCollector(BrowserPool pool, ILogger<PichauCollector> logger) : base(pool, logger)
    {
    }

    protected override string StoreDomain => "www.pichau.com.br";

    protected override string StoreSlugValue => "pichau";

    protected override string CategoryPath(string categorySlug) => categorySlug switch
    {
        "cpu" => "hardware/processadores",
        "gpu" => "hardware/placa-de-video",
        "motherboard" => "hardware/placa-mae",
        "memory" => "hardware/memoria-ram",
        "storage" => "hardware/ssd",
        "psu" => "hardware/fonte",
        "case" => "hardware/gabinete",
        "cooler" => "hardware/water-cooler",
        _ => throw new NotSupportedException($"Pichau: categoria '{categorySlug}' sem rota."),
    };

    // slugs de produto: /processador-{...}, /placa-de-video-{...} etc.
    protected override Regex ProductHref => ProductHrefRegex();

    protected override Regex PriceRegex => PriceRegexLocal();

    protected override string PriceMarker => "de r$";

    protected override string ExtractStoreSku(string href)
    {
        var sku = href.TrimEnd('/').Split('/').Last();
        return sku.Length > 255 ? sku[..255] : sku;
    }

    protected override async Task<bool> GoNextPageAsync(IPage page, int nextPage, string? prevFirstHref, CancellationToken ct = default)
    {
        var clickJs =
            "() => { const a = Array.from(document.querySelectorAll('a[href]'))" +
            $".find(x => x.getAttribute('href')?.endsWith('?page={nextPage}')); " +
            "if (a) { a.click(); return true; } return false; }";
        var clicked = await page.EvaluateAsync<bool>(clickJs);
        if (!clicked)
            return false;

        // aguarda o primeiro produto da página mudar (navegação SPA)
        try
        {
            var waitJs =
                "(prev) => { const a = document.querySelector(" +
                "'a[href*=\"/processador-\"], a[href*=\"/placa-de-video-\"], " +
                "a[href*=\"/placa-mae-\"], a[href*=\"/memoria-\"], a[href*=\"/ssd-\"], " +
                "a[href*=\"/fonte-\"], a[href*=\"/gabinete-\"], a[href*=\"/water-cooler-\"]'); " +
                "return a && a.href !== prev; }";
            await page.WaitForFunctionAsync(waitJs, prevFirstHref, new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            return false;
        }
        await Task.Delay(1500);
        return true;
    }

    [GeneratedRegex(@"/(?:processador|placa-de-video|placa-mae|memoria|ssd|fonte|gabinete|water-cooler)-[a-z0-9-]+", RegexOptions.IgnoreCase)]
    private static partial Regex ProductHrefRegex();

    [GeneratedRegex(@"por\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegexLocal();
}
