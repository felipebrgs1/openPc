using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Collector da Kabum via HTTP puro — sem anti-bot (validado no M1).
/// Fonte: `__NEXT_DATA__` SSR da listagem; specs da ficha técnica vêm da
/// página de produto quando a categoria as exige (CPU/GPU).
/// </summary>
public sealed partial class KabumCollector(HttpClient http, ILogger<KabumCollector> logger)
    : IStoreCollector
{
    public string StoreSlug => "kabum";

    // Rotas validadas via sitemap (2026-08-08). TODO(M2+): rota de gabinete
    // da Kabum não está no sitemap — descobrir (ex: /hardware/gabinetes/...).
    private static readonly IReadOnlyDictionary<string, string> CategoryPaths = new Dictionary<string, string>
    {
        ["cpu"] = "hardware/processadores",
        ["motherboard"] = "hardware/placas-mae",
        ["gpu"] = "hardware/placa-de-video-vga",
        ["memory"] = "hardware/memoria-ram",
        ["storage"] = "hardware/ssd-2-5",
        ["psu"] = "hardware/fontes",
        ["cooler"] = "hardware/coolers",
    };

    public async Task<IReadOnlyList<RawListing>> CollectAsync(string categorySlug, CancellationToken ct)
    {
        if (!CategoryPaths.TryGetValue(categorySlug, out var path))
            throw new NotSupportedException($"Kabum: categoria '{categorySlug}' sem rota mapeada.");

        // Specs essenciais (socket, cores, threads, iGPU, part number) saem do
        // título da listagem — a ficha técnica completa (página de produto)
        // exige 1 request por SKU (~600) e fica para um job de enrichment (M3).
        var products = new List<RawListing>();
        var page = 1;

        while (true)
        {
            var items = await FetchPageAsync(path, page, ct);
            if (items.Count == 0)
                break;

            foreach (var raw in items)
                products.Add(BuildListing(raw, categorySlug));

            // 10 páginas × 60 produtos; para o catálogo completo paramos ao fim
            if (items.Count < 60)
                break;
            page++;
            await Task.Delay(Random.Shared.Next(1500, 2500), ct); // rate limit conservador
        }

        logger.LogInformation("Kabum/{Category}: {Count} produtos coletados", categorySlug, products.Count);
        return products;
    }

    private async Task<List<KabumListItem>> FetchPageAsync(string path, int page, CancellationToken ct)
    {
        var url = $"https://www.kabum.com.br/{path}?page_number={page}";
        using var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync(ct);
        return KabumPageParser.ParseListingsPage(html);
    }

    private RawListing BuildListing(KabumListItem p, string categorySlug)
    {
        var url = $"https://www.kabum.com.br/produto/{p.Code}/{p.FriendlyName}";

        var specs = categorySlug switch
        {
            "cpu" => SpecExtractor.ExtractCpu(p.Title, null),
            "gpu" => SpecExtractor.ExtractGpu(p.Title, null),
            "motherboard" => SpecExtractor.ExtractMotherboard(p.Title),
            "memory" => SpecExtractor.ExtractMemory(p.Title),
            _ => new Dictionary<string, string>(),
        };

        return new RawListing(
            StoreSku: p.Code.ToString(),
            Title: p.Title,
            Url: url,
            PriceCash: p.PriceWithDiscount ?? p.Price,
            PriceCard: null,
            Installments: ParseInstallments(p.MaxInstallment),
            InstallmentText: p.MaxInstallment,
            InStock: p.Available,
            Thumbnail: p.Thumbnail,
            Manufacturer: p.Manufacturer,
            PartNumber: PartNumber.Extract(p.Title),
            MatchKey: MatchKey.Build(p.Title),
            Specs: specs);
    }

    private static int? ParseInstallments(string? text)
    {
        if (text is null)
            return null;
        var m = Regex.Match(text, @"(\d{1,3})x");
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }
}
