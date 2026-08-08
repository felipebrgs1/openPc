using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Terabyte (Cloudflare). Cards com "De: R$ X por: R$ Y" (PIX). Catálogo
/// inteiro em uma página (148 produtos em processadores) — sem paginação.
/// </summary>
public sealed partial class TerabyteCollector : BrowserCollectorBase
{
    public TerabyteCollector(BrowserPool pool, ILogger<TerabyteCollector> logger) : base(pool, logger)
    {
    }

    protected override string StoreDomain => "www.terabyteshop.com.br";

    protected override string StoreSlugValue => "terabyte";

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
        _ => throw new NotSupportedException($"Terabyte: categoria '{categorySlug}' sem rota."),
    };

    // URLs: /produto/{id}/{slug}
    protected override Regex ProductHref => ProductHrefRegex();

    protected override Regex PriceRegex => PriceRegexLocal();

    protected override string PriceMarker => "de:";

    protected override string ExtractStoreSku(string href)
    {
        var m = Regex.Match(href, @"/produto/(\d+)");
        return m.Success ? m.Groups[1].Value : href;
    }

    [GeneratedRegex(@"/produto/\d+/[a-z0-9-]+", RegexOptions.IgnoreCase)]
    private static partial Regex ProductHrefRegex();

    [GeneratedRegex(@"por:\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegexLocal();
}
