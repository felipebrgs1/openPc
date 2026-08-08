using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

// Spike M1 — Pichau e Terabyte via Playwright (Chromium real):
// o Cloudflare exige browser; valida extração em volume dos cards e mede
// taxa de sucesso + timing. NÃO é o collector de produção (M2).

public static class PlaywrightProbe
{
    private const string Ua =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36";

    public static async Task RunAsync()
    {

var stores = new (string Name, string Url, Regex CardRegex)[]
{
    ("pichau", "https://www.pichau.com.br/hardware/processadores",
        new Regex(@"por\s*R\$\s*([\d.,]+)", RegexOptions.IgnoreCase)),
    ("terabyte", "https://www.terabyteshop.com.br/hardware/processadores",
        new Regex(@"por:\s*R\$\s*([\d.,]+)", RegexOptions.IgnoreCase)),
};

using var pw = await Playwright.CreateAsync();
await using var browser = await pw.Chromium.LaunchAsync(new()
{
    Headless = true,
    // Chromium completo (new headless), não o headless-shell: menos detectável
    Channel = "chromium",
    Args = ["--disable-blink-features=AutomationControlled"],
});

foreach (var (name, url, priceRegex) in stores)
{
    var sw = Stopwatch.StartNew();
    var cards = new List<(string Href, string Text)>();
    string? error = null;

    try
    {
        var page = await browser.NewPageAsync(new() { UserAgent = Ua });
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45_000 });

        // aguarda o desafio do Cloudflare passar e a página renderizar
        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('a[href]').length > 50", null, new() { Timeout = 45_000 });

        // scroll progressivo para forçar lazy-load
        for (var i = 0; i < 12; i++)
        {
            await page.EvaluateAsync("window.scrollBy(0, 900)");
            await Task.Delay(400);
        }
        await Task.Delay(2500);

        var state = await page.EvaluateAsync<JsonElement>(
            "() => ({ title: document.title, total: document.querySelectorAll('a[href]').length, sample: Array.from(document.querySelectorAll('a[href]')).slice(0, 30).map(a => a.href) })");
        Console.WriteLine($"debug: title='{state.GetProperty("title").GetString()}' totalAnchors={state.GetProperty("total").GetInt32()} sample={string.Join(" | ", state.GetProperty("sample").EnumerateArray().Take(3).Select(x => x.GetString()))}");

        var anchors = await page.EvaluateAsync<JsonElement>(
            "() => Array.from(document.querySelectorAll('a[href]'))" +
            ".filter(a => /processador|produto/i.test(a.href))" +
            ".map(a => { let n = a; for (let i = 0; i < 5 && n.parentElement; i++) { n = n.parentElement; const t = n.innerText || ''; if (t.includes('R$') && t.length > 80) break; } return { href: a.href, text: n.innerText || '' }; })");

        foreach (var el in anchors.EnumerateArray())
        {
            var href = el.GetProperty("href").GetString() ?? "";
            var text = el.GetProperty("text").GetString() ?? "";
            if (priceRegex.IsMatch(text) && !cards.Any(c => c.Href == href))
                cards.Add((href, text));
        }
    }
    catch (Exception ex)
    {
        error = ex.Message;
    }

    sw.Stop();
    var withPrice = cards.Count(c => priceRegex.IsMatch(c.Text));

    Console.WriteLine();
    Console.WriteLine($"=== RESUMO {name.ToUpperInvariant()} (PLAYWRIGHT) ===");
    Console.WriteLine($"cards com preço: {withPrice}/{cards.Count} | erro: {error ?? "nenhum"}");
    Console.WriteLine($"tempo: {sw.Elapsed.TotalSeconds:F1}s | {cards.Count / Math.Max(sw.Elapsed.TotalSeconds, 0.1):F1} cards/s");

    var sample = cards.FirstOrDefault();
    if (sample.Href is not null)
    {
        var m = priceRegex.Match(sample.Text);
        Console.WriteLine($"amostra: {sample.Href[..Math.Min(90, sample.Href.Length)]}");
        Console.WriteLine($"  preço capturado: R$ {m.Groups[1].Value}");
        Console.WriteLine($"  nome: {sample.Text[..Math.Min(90, sample.Text.Length)].ReplaceLineEndings(" | ")}");
    }
}
}
}
