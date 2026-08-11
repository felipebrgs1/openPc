using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Terabyte (Cloudflare). Cards com "De: R$ X por: R$ Y" (PIX). Catálogo em
/// uma página com carregamento em lote: botão "CLIQUE PARA VER MAIS PRODUTOS"
/// (+30 itens por clique) até esgotar — ver <see cref="GoNextPageAsync"/>.
/// </summary>
public sealed partial class TerabyteCollector : BrowserCollectorBase
{
    public TerabyteCollector(BrowserPool pool, ILogger<TerabyteCollector> logger) : base(pool, logger)
    {
    }

    protected override string StoreDomain => "www.terabyteshop.com.br";

    protected override string StoreSlugValue => "terabyte";

    // Rotas atuais (2026-08-11 — redesign mudou as URLs de hardware/ para
    // top-level em fontes/gabinetes/refrigeracao e pluralizou placas-*).
    // As antigas (ex: hardware/placa-de-video) redirecionam para /hardware
    // (página genérica) ou para a home — nunca colete a partir delas.
    protected override string CategoryPath(string categorySlug) => categorySlug switch
    {
        "cpu" => "hardware/processadores",
        "gpu" => "hardware/placas-de-video",
        "motherboard" => "hardware/placas-mae",
        "memory" => "hardware/memorias",
        "storage" => "hardware/hard-disk",
        "psu" => "fontes",
        "case" => "gabinetes",
        "cooler" => "refrigeracao",
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

    /// <summary>
    /// Paginação por botão "CLIQUE PARA VER MAIS PRODUTOS" (classes
    /// btn-pdmore/tfv2-more): cada clique renderiza +30 cards e o botão some
    /// quando o catálogo acaba. Clique via JS — o Playwright não consegue
    /// clicar (elemento instável por animação).
    ///
    /// Observado no site (2026-08-11): os primeiros 4 cliques são no-op
    /// (anti-bot/fila) e o 5º carrega o primeiro lote — por isso tentamos
    /// vários cliques sem progresso antes de declarar fim da listagem.
    /// Retorna true no PRIMEIRO clique que carrega produtos: a base extrai
    /// os cards entre cliques (um clique por chamada).
    /// </summary>
    protected override async Task<bool> GoNextPageAsync(IPage page, int nextPage, string? prevFirstHref, CancellationToken ct = default)
    {
        var more = page.Locator(".btn-pdmore, .tfv2-more").First;
        if (!await more.IsVisibleAsync())
            return false;

        var noProgress = 0;
        while (noProgress < 6)
        {
            var before = await CountProductsAsync(page);
            await more.EvaluateAsync("el => el.click()");
            await Task.Delay(2500, ct);
            var after = await CountProductsAsync(page);

            if (after > before)
                return true; // lote carregado — base extrai e chama de novo
            noProgress++;
        }

        _logger.LogWarning("Terabyte: {Clicks} cliques em 'ver mais' sem produtos novos ({Count}) — fim da listagem",
            noProgress, await CountProductsAsync(page));
        return false;
    }

    private static Task<int> CountProductsAsync(IPage page)
        => page.EvaluateAsync<int>(
            "() => new Set(Array.from(document.querySelectorAll('a[href]'))" +
            ".map(a => a.href).filter(h => /\\/produto\\//.test(h))).size");

    [GeneratedRegex(@"/produto/\d+/[a-z0-9-]+", RegexOptions.IgnoreCase)]
    private static partial Regex ProductHrefRegex();

    [GeneratedRegex(@"por:\s*\|?\s*R\$\s*([\d.]+,[\d]{2})", RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegexLocal();
}
