using System.Text.RegularExpressions;
using OpenPc.Scraper.Normalization;

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Constrói um RawListing a partir do card renderizado de uma loja browser
/// (Pichau/Terabyte). Isolado para testes com fixtures de texto real.
/// </summary>
public static class CardListingBuilder
{
    public static RawListing? Build(
        string href, string cardText, string categorySlug,
        Regex priceRegex, string priceMarker, Func<string, string> extractStoreSku)
    {
        var m = priceRegex.Match(cardText);
        if (!m.Success)
            return null;
        var price = PriceParser.ParseBr(m.Groups[1].Value);
        if (price is null)
            return null;

        // nome = linha mais longa antes do bloco de preço (badges ficam em linhas curtas)
        var head = cardText.Split(priceMarker, 2)[0];
        var name = head.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .OrderByDescending(l => l.Length)
            .FirstOrDefault() ?? "";

        var installment = Regex.Match(cardText, @"(\d{1,3})x\s*de\s*R\$\s*[\d.,]+");
        var specs = categorySlug switch
        {
            "cpu" => SpecExtractor.ExtractCpu(name, null),
            "gpu" => SpecExtractor.ExtractGpu(name, null),
            "motherboard" => SpecExtractor.ExtractMotherboard(name),
            _ => new Dictionary<string, string>(),
        };

        return new RawListing(
            StoreSku: extractStoreSku(href),
            Title: name,
            Url: href,
            PriceCash: price,
            PriceCard: null,
            Installments: installment.Success ? int.Parse(installment.Groups[1].Value) : null,
            InstallmentText: installment.Success ? installment.Value.Trim() : null,
            InStock: !cardText.Contains("esgotado", StringComparison.OrdinalIgnoreCase),
            Thumbnail: null,
            Manufacturer: null,
            PartNumber: PartNumber.Extract(name + " " + href),
            MatchKey: MatchKey.Build(name),
            Specs: specs);
    }
}
