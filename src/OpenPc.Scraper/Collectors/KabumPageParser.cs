using System.Text.RegularExpressions;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenPc.Scraper.Tests")]

namespace OpenPc.Scraper.Collectors;

/// <summary>
/// Parser da página de listagem da Kabum (isolado do collector para testes).
/// Entrada: HTML com __NEXT_DATA__; saída: itens brutos da listagem.
/// </summary>
public static partial class KabumPageParser
{
    public static List<KabumListItem> ParseListingsPage(string html)
    {
        var m = NextDataRegex().Match(html);
        if (!m.Success)
            throw new InvalidOperationException("Kabum: __NEXT_DATA__ ausente na página (bloqueio?).");

        using var doc = System.Text.Json.JsonDocument.Parse(m.Groups[1].Value);
        var data = doc.RootElement
            .GetProperty("props").GetProperty("pageProps").GetProperty("data").GetString()
            ?? throw new InvalidOperationException("Kabum: pageProps.data vazio.");

        using var inner = System.Text.Json.JsonDocument.Parse(data);
        return inner.RootElement.GetProperty("catalogServer").GetProperty("data")
            .EnumerateArray()
            .Select(el => new KabumListItem(
                Code: el.GetProperty("code").GetInt32(),
                Title: el.GetProperty("name").GetString() ?? "",
                FriendlyName: el.GetProperty("friendlyName").GetString() ?? "",
                Manufacturer: el.TryGetProperty("manufacturer", out var man)
                    && man.ValueKind == System.Text.Json.JsonValueKind.Object
                    && man.TryGetProperty("name", out var manName)
                        ? manName.GetString() : null,
                PriceWithDiscount: el.TryGetProperty("priceWithDiscount", out var pwd) && pwd.GetDecimal() > 0
                    ? pwd.GetDecimal() : null,
                Price: el.GetProperty("price").GetDecimal(),
                MaxInstallment: el.TryGetProperty("maxInstallment", out var mi) ? mi.GetString() : null,
                Available: el.GetProperty("available").GetBoolean(),
                Thumbnail: el.TryGetProperty("thumbnail", out var th) ? th.GetString() : null))
            .ToList();
    }

    [GeneratedRegex(@"<script id=""__NEXT_DATA__"" type=""application/json"">(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex NextDataRegex();
}

public sealed record KabumListItem(
    int Code,
    string Title,
    string FriendlyName,
    string? Manufacturer,
    decimal? PriceWithDiscount,
    decimal Price,
    string? MaxInstallment,
    bool Available,
    string? Thumbnail);
