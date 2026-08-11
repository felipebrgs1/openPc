using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Base de conhecimento curada de consumo (docs/specs.md §4.3/§4.4): fallback
/// de `tdp_w` quando a ficha técnica não foi extraída do scraping (o job de
/// enrichment ainda não existe e títulos de listagem raramente citam TDP).
/// Dado editorial versionado no repo (Infrastructure/Seeds/tdp.json) — o
/// scraper NÃO preenche isto. Revisar a cada geração nova de hardware.
/// </summary>
public sealed class TdpSeed
{
    public required IReadOnlyList<TdpEntry> Entries { get; init; }

    /// <summary>
    /// Primeiro match vence — a ordem do seed importa (variantes específicas
    /// antes da base). Tenta o nome completo primeiro (distingue variantes
    /// "4070 super/ti", "9060 xt 16gb/8gb" que o modelo normalizado colapsa),
    /// depois o modelo normalizado ("amd 5700", "nvidia 4070", "amd 9060xt").
    /// </summary>
    public decimal? Find(PartCategory category, string? model, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalized = Normalize(name);
            foreach (var entry in Entries)
            {
                if (entry.Category == category && entry.Target == TdpTarget.Name &&
                    Regex.IsMatch(normalized, $@"\b(?:{entry.Pattern})\b", RegexOptions.IgnoreCase))
                    return entry.Watts;
            }
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            foreach (var entry in Entries)
            {
                if (entry.Category == category && entry.Target == TdpTarget.Model &&
                    Regex.IsMatch(model, $@"^(?:{entry.Pattern})$", RegexOptions.IgnoreCase))
                    return entry.Watts;
            }
        }

        return null;
    }

    /// <summary>Lowercase, sem acento, só alfanumérico/espaços (mesma regra do MatchKey).</summary>
    private static string Normalize(string value)
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

public enum TdpTarget
{
    /// <summary>Padrão casado contra o nome completo normalizado (ex: "rtx 4070 super").</summary>
    Name,

    /// <summary>Padrão casado contra o modelo normalizado (ex: "amd 5700", "nvidia 4070").</summary>
    Model,
}

public sealed record TdpEntry(PartCategory Category, TdpTarget Target, string Pattern, decimal Watts);
