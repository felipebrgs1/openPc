using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenPc.Scraper.Normalization;

/// <summary>
/// Extração de specs estruturadas (EAV) a partir do título e da ficha técnica
/// em texto. O contrato de chaves é o mesmo das specs (docs/specs.md §3.2).
/// Spec ausente = chave omitida (a engine M3 trata como "desconhecido").
/// </summary>
public static partial class SpecExtractor
{
    public static IReadOnlyDictionary<string, string> ExtractCpu(string title, string? specText)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = specText ?? "";

        Set(specs, "socket", (FirstMatch(text, Socket()) ?? FirstMatch(title, Socket()))?.ToLowerInvariant());
        Set(specs, "tdp_w", FirstMatch(text, Tdp()) ?? FirstMatch(title, Tdp()));
        Set(specs, "cores", FirstMatch(title, Cores()) ?? FirstMatch(text, Cores()));
        Set(specs, "threads", FirstMatch(title, Threads()) ?? FirstMatch(text, Threads()));
        Set(specs, "memory_type", FirstMatch(text, MemoryType()) ?? FirstMatch(title, MemoryType()));
        Set(specs, "has_igpu", DetectIgpu(title, text)?.ToString().ToLowerInvariant());

        return specs;
    }

    public static IReadOnlyDictionary<string, string> ExtractGpu(string title, string? specText)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = specText ?? "";

        Set(specs, "memory_gb", FirstMatch(title, GpuMemoryGb()) ?? FirstMatch(text, GpuMemoryGb()));
        Set(specs, "tdp_w", FirstMatch(text, Tdp()) ?? FirstMatch(title, Tdp()));
        Set(specs, "length_mm", FirstMatch(text, DimensionsMm()));
        Set(specs, "power_connectors", FirstMatch(text, PowerConnectors()));

        return specs;
    }

    private static void Set(IDictionary<string, string> specs, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            specs[key] = value.Trim();
    }

    private static string? FirstMatch(string text, Regex regex) =>
        regex.Match(text) is { Success: true } m ? m.Groups[1].Value.Trim() : null;

    private static bool? DetectIgpu(string title, string specText)
    {
        if (Regex.IsMatch(title + " " + specText, @"sem v[íi]deo|without graphics|no i?gpu|sem placa", RegexOptions.IgnoreCase))
            return false;
        if (Regex.IsMatch(title + " " + specText, @"com v[íi]deo|gr[áa]ficos integrados|integrated graphics|radeon graphics|uhd graphics|hd graphics|iris", RegexOptions.IgnoreCase))
            return true;
        return null;
    }

    [GeneratedRegex(@"\b(AM5|AM4|LGA\s?1?[0-9]{3}|sTR5|sTRX50|sTRX40)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Socket();

    [GeneratedRegex(@"(?:TDP|consumo|pot[eê]ncia)[^0-9]{0,20}(\d{2,4})\s*W", RegexOptions.IgnoreCase)]
    private static partial Regex Tdp();

    [GeneratedRegex(@"(\d{1,2})\s*[Nn][úu]cleos?", RegexOptions.IgnoreCase)]
    private static partial Regex Cores();

    [GeneratedRegex(@"(\d{1,3})\s*[Tt]hreads?", RegexOptions.IgnoreCase)]
    private static partial Regex Threads();

    [GeneratedRegex(@"\b(DDR[45])\b", RegexOptions.IgnoreCase)]
    private static partial Regex MemoryType();

    [GeneratedRegex(@"(\d{2,3})\s*GB", RegexOptions.IgnoreCase)]
    private static partial Regex GpuMemoryGb();

    [GeneratedRegex(@"(\d{3})\s*[xX]\s*\d{2,3}\s*[xX]\s*\d{1,3}\s*mm", RegexOptions.IgnoreCase)]
    private static partial Regex DimensionsMm();

    [GeneratedRegex(@"(?:conectores|alimenta[çc][ãa]o|power)[^0-9]{0,20}(\d+\s*x\s*\d+\s*(?:pinos?|pin)|16\s*pinos?|8\s*pinos?|12V-?2x?6?)", RegexOptions.IgnoreCase)]
    private static partial Regex PowerConnectors();
}
