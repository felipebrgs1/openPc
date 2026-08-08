using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenPc.Scraper.Normalization;

/// <summary>
/// Chave determinística de matching (nível 2 do dedup): marca + modelo
/// numérico com sufixo de variante. Ex.: "ryzen 7600x", "core i5 12400f",
/// "rtx 5070", "radeon 7800xt". Categorias sem padrão conhecido retornam null.
/// </summary>
public static partial class MatchKey
{
    private static readonly (Regex Regex, string Prefix)[] Patterns =
    [
        (CpuAmd(), "amd"),
        (CpuIntel(), "intel"),
        (GpuNvidia(), "nvidia"),
        (GpuAmd(), "amd"),
    ];

    [GeneratedRegex(@"\bryzen\s+(?:[3579]\s*)?(\d{3,5}[a-z0-9]*)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CpuAmd();

    [GeneratedRegex(@"\b(?:core\s+)?(?:ultra\s+)?(i[3579]|ultra\s*[579])\s*-?\s*(\d{3,5}[a-z0-9]*)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CpuIntel();

    [GeneratedRegex(@"\b(?:rtx|gtx|titan)\s*(\d{3,4}[a-z0-9]*)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GpuNvidia();

    [GeneratedRegex(@"\b(?:radeon\s+)?rx\s*(\d{3,4}[a-z0-9]*\s*(?:xt|gre)?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GpuAmd();

    public static string? Build(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var text = Normalize(title);
        foreach (var (regex, prefix) in Patterns)
        {
            var m = regex.Match(text);
            if (m.Success)
                return $"{prefix} {m.Groups[^1].Value.Replace(" ", "")}";
        }
        return null;
    }

    /// <summary>lowercase, sem acento, espaços preservados/colapsados.</summary>
    public static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(value.Length);
        foreach (var c in decomposed)
        {
            if (char.IsLetterOrDigit(c) || c == ' ')
                sb.Append(char.ToLowerInvariant(c));
        }
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
}
