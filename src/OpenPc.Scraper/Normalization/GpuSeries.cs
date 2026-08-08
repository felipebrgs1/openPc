using System.Text.RegularExpressions;

namespace OpenPc.Scraper.Normalization;

/// <summary>
/// Série/geração da GPU a partir do título — usado como filtro de catálogo
/// (atributo `series`, chave não usada pela engine). Valores canônicos:
/// rtx20/rtx30/rtx40/rtx50, gtx16, rx5000/rx6000/rx7000/rx9000, arc.
/// Fora do padrão (Quadro/pro, títulos sem família) retorna null — o produto
/// aparece só em "Todos".
/// </summary>
public static partial class GpuSeries
{
    public static string? Classify(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var t = title.ToLowerInvariant();

        var rtx = Rtx().Match(t);
        if (rtx.Success)
            return rtx.Groups[1].Value[0] switch
            {
                '2' => "rtx20",
                '3' => "rtx30",
                '4' => "rtx40",
                '5' => "rtx50",
                _ => null, // ex.: "RTX 9070" (typo de loja) — fora do padrão
            };

        // GTX restantes no catálogo são 16xx (1630/1650/1660 — GDDR6/GDDR5X)
        if (Gtx().Match(t).Success)
            return "gtx16";

        // "rx 9070" e "radeon 9070" (títulos sem o prefixo "rx")
        var rx = Rx().Match(t);
        if (rx.Success)
            return rx.Groups[1].Value[0] switch
            {
                '5' => "rx5000",
                '6' => "rx6000",
                '7' => "rx7000",
                '9' => "rx9000",
                _ => null,
            };

        if (Arc().Match(t).Success)
            return "arc";

        return null;
    }

    [GeneratedRegex(@"\brtx\s*(\d{4})")]
    private static partial Regex Rtx();

    [GeneratedRegex(@"\bgtx\s*16\d{2}")]
    private static partial Regex Gtx();

    [GeneratedRegex(@"\b(?:rx|radeon)\s*(\d{4})")]
    private static partial Regex Rx();

    [GeneratedRegex(@"\barc\s*[ab]\d{3}")]
    private static partial Regex Arc();
}
