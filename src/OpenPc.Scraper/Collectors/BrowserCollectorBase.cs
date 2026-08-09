using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Base para lojas com Cloudflare (Pichau, Terabyte): navega com Chromium
/// real, resolve o desafio, scrolla para forçar lazy-load e extrai cards de
/// produto do DOM renderizado. Preços vêm do texto do card (padrões
/// validados no spike M1).
/// </summary>
public abstract partial class BrowserCollectorBase : IStoreCollector
{
    protected BrowserCollectorBase(BrowserPool pool, ILogger logger)
    {
        _pool = pool;
        _logger = logger;
    }

    protected abstract string StoreDomain { get; }
    /// <summary>Slug da loja no banco (seed de stores).</summary>
    protected abstract string StoreSlugValue { get; }
    protected abstract string CategoryPath(string categorySlug);
    /// <summary>Regex que identifica href de produto na listagem.</summary>
    protected abstract Regex ProductHref { get; }
    /// <summary>Regex do preço à vista no texto do card (grupo 1).</summary>
    protected abstract Regex PriceRegex { get; }
    /// <summary>Marcador de início do bloco de preço (para isolar o nome).</summary>
    protected abstract string PriceMarker { get; }
    /// <summary>Extrai o SKU da loja a partir do href do produto.</summary>
    protected abstract string ExtractStoreSku(string href);

    private readonly BrowserPool _pool;
    private readonly ILogger _logger;

    public string StoreSlug => StoreSlugValue;

    public async Task<IReadOnlyList<RawListing>> CollectAsync(string categorySlug, CancellationToken ct)
    {
        var url = $"https://{StoreDomain}/{CategoryPath(categorySlug)}";
        var all = new List<RawListing>();
        var page = await _pool.NewPageAsync();
        try
        {
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45_000 });
            await WaitCloudflareAsync(page);

            var pageNumber = 1;
            while (true)
            {
                await ScrollToLoadAsync(page);
                all.AddRange(await ExtractCardsAsync(page, categorySlug));

                var prevFirst = all.FirstOrDefault()?.Url;
                if (!await GoNextPageAsync(page, pageNumber + 1, prevFirst))
                    break;
                pageNumber++;
            }
        }
        finally
        {
            await page.CloseAsync();
        }

        var distinct = all.GroupBy(l => l.StoreSku).Select(g => g.First()).ToList();
        _logger.LogInformation("{Store}/{Category}: {Count} produtos coletados ({Total} cards)",
            StoreSlug, categorySlug, distinct.Count, all.Count);
        return distinct;
    }

    /// <summary>Avança para a próxima página (SPA); false = fim da listagem.</summary>
    protected virtual Task<bool> GoNextPageAsync(IPage page, int nextPage, string? prevFirstHref, CancellationToken ct = default)
        => Task.FromResult(false);

    private async Task WaitCloudflareAsync(IPage page)
    {
        // aguarda o desafio do Cloudflare passar (a página renderiza nav + cards)
        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('a[href]').length > 50", null,
            new() { Timeout = 45_000 });
        await Task.Delay(1500);
    }

    private static async Task ScrollToLoadAsync(IPage page)
    {
        for (var i = 0; i < 12; i++)
        {
            await page.EvaluateAsync("window.scrollBy(0, 900)");
            await Task.Delay(400);
        }
        await Task.Delay(2000);
    }

    private async Task<IReadOnlyList<RawListing>> ExtractCardsAsync(IPage page, string categorySlug)
    {
        var pattern = ProductHref.ToString().Replace(@"\", @"\\");
        var marker = PriceMarker;
        var js =
            "() => Array.from(document.querySelectorAll('a[href]'))" +
            $".filter(a => new RegExp('{pattern}').test(a.href))" +
            ".map(a => { let n = a; for (let i = 0; i < 5 && n.parentElement; i++) {" +
            " n = n.parentElement; if (n.querySelectorAll('a[href]').length > 2) break;" + // grid, não card
            " const t = n.innerText || '';" +
            " if (t.includes('R$') && t.length > 80) break; }" +
            " const t = n.innerText || '';" +
            " const img = n.querySelector('img');" +
            " const src = img ? (img.currentSrc || img.src || img.dataset.src || img.dataset.original || '') : '';" +
            " return { href: a.href, text: n.querySelectorAll('a[href]').length > 2 ? '' : t, img: src }; })";
        var cards = await page.EvaluateAsync<JsonElement>(js);

        var result = new List<RawListing>();
        foreach (var el in cards.EnumerateArray())
        {
            var href = el.GetProperty("href").GetString() ?? "";
            var text = el.GetProperty("text").GetString() ?? "";
            var img = el.TryGetProperty("img", out var i) ? i.GetString() : null;
            var listing = BuildListing(href, text, img, categorySlug, marker);
            if (listing is not null)
                result.Add(listing);
        }
        return result;
    }

    private RawListing? BuildListing(string href, string cardText, string? thumbnail, string categorySlug, string priceMarker)
        => CardListingBuilder.Build(href, cardText, categorySlug, PriceRegex, priceMarker, ExtractStoreSku, thumbnail);
}
