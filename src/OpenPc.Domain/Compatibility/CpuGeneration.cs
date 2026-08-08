using System.Text.RegularExpressions;

namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Classifica a geração da CPU a partir do modelo (match key normalizado,
/// ex: "amd 7600x", "intel 12400f", "intel 265f"). Os códigos retornados são
/// os usados na matriz de chipsets (compatibility.json): zen1..zen5,
/// alder-lake, raptor-lake, raptor-lake-refresh, arrow-lake.
/// </summary>
public static partial class CpuGeneration
{
    public static string? Classify(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return null;

        var m = model.ToLowerInvariant();

        if (m.Contains("ryzen") || m.StartsWith("amd"))
        {
            // AMD fora da matriz desktop: Athlon, A-series (A10-5800K, A8-7600),
            // FX, Sempron, Phenom, Opteron. Só "startsWith amd" não basta:
            // "Athlon 3000g" tem 4 dígitos (daria zen2) e "A10-5800K" tem
            // "5800" (daria zen3) — ambos classificados errado. Títulos com
            // ruído de loja ("Amd Ryzen Athlon 3000g") caem aqui também.
            if (m.Contains("athlon") || m.Contains("sempron") || m.Contains("phenom")
                || m.Contains("opteron") || AmdSeries().IsMatch(m) || FxSeries().IsMatch(m))
                return null;

            var four = FourDigit().Match(m);
            if (!four.Success)
                return null;
            return four.Groups[1].Value[0] switch
            {
                '1' => "zen1",                 // Ryzen 1000 (Summit Ridge)
                '2' or '3' or '4' => "zen2",   // 2000 (Zen+), 3000, 4000G
                '5' => "zen3",
                '6' => null,                   // Ryzen 6000 é mobile — fora da matriz desktop
                '7' or '8' => "zen4",          // 7000 (Raphael), 8000G (Phoenix)
                '9' => "zen5",
                _ => null,
            };
        }

        if (m.Contains("core") || m.Contains("ultra") || m.StartsWith("intel"))
        {
            var five = IntelFamily().Match(m);
            if (five.Success)
                return five.Groups[1].Value[..2] switch
                {
                    "12" => "alder-lake",
                    "13" => "raptor-lake",
                    "14" => "raptor-lake-refresh",
                    _ => null,
                };

            // Core Ultra 2xx (desktop, LGA1851)
            if (UltraFamily().Match(m).Success)
                return "arrow-lake";

            return null;
        }

        return null;
    }

    [GeneratedRegex(@"(?<!\d)(\d{4})(?!\d)")]
    private static partial Regex FourDigit();

    /// <summary>A-series da AMD: A4/A6/A8/A9/A10/A12 (ex.: "A10-5800K", "a8-7600").</summary>
    [GeneratedRegex(@"\ba\d{1,2}\b")]
    private static partial Regex AmdSeries();

    /// <summary>FX da AMD (ex.: "FX-8350", "FX-9590").</summary>
    [GeneratedRegex(@"\bfx\b")]
    private static partial Regex FxSeries();

    [GeneratedRegex(@"(?<!\d)(1[2-4]\d{3})(?!\d)")]
    private static partial Regex IntelFamily();

    [GeneratedRegex(@"(?<!\d)(2\d{2})(?!\d)")]
    private static partial Regex UltraFamily();
}
