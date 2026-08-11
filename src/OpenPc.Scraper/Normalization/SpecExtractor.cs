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

        Set(specs, "socket", NormalizeSocket(FirstMatch(text, Socket()) ?? FirstMatch(title, Socket())));
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
        Set(specs, "series", GpuSeries.Classify(title)); // filtro de catálogo, não engine

        return specs;
    }

    /// <summary>
    /// Specs essenciais de memória RAM a partir do título. Chaves do contrato
    /// §3.2; hoje só `type` (ddr4|ddr5) é extraído — alimenta o filtro de
    /// catálogo e futuras regras da engine (RAM_TYPE_MISMATCH).
    /// </summary>
    public static IReadOnlyDictionary<string, string> ExtractMemory(string title)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Set(specs, "type", FirstMatch(title, MemoryType())?.ToLowerInvariant());
        return specs;
    }

    /// <summary>
    /// Specs essenciais de fonte a partir do título: `wattage` (alimenta a
    /// regra PSU_WATTAGE_LOW). Títulos de fonte quase sempre citam a potência
    /// ("Fonte 500W") — diferente de CPU/GPU, onde o TDP só sai da ficha.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ExtractPsu(string title)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Set(specs, "wattage", FirstMatch(title, PsuWattage()));
        return specs;
    }

    /// <summary>
    /// Specs essenciais de placa-mãe a partir do título (as 3 lojas embutem
    /// socket, chipset, formato e DDR no nome). Chaves do contrato §3.2 que
    /// alimentam a engine M3.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ExtractMotherboard(string title)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Set(specs, "socket", NormalizeSocket(FirstMatch(title, Socket())));
        Set(specs, "chipset", NormalizeChipset(FirstMatch(title, Chipset())));
        Set(specs, "form_factor", NormalizeFormFactor(FirstMatch(title, FormFactor())));
        Set(specs, "memory_type", FirstMatch(title, MemoryType())?.ToLowerInvariant());

        return specs;
    }

    /// <summary>
    /// Socket canônico: minúsculas e sem espaço ("LGA 1700" → "lga1700") —
    /// a engine compara o valor bruto; "lga 1700" vs "lga1700" não pode
    /// divergir na mesma plataforma.
    /// </summary>
    private static string? NormalizeSocket(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.ToLowerInvariant().Replace(" ", "");

    /// <summary>Contrato da engine (§3.2): atx | matx | itx | eatx — Mini-ITX vira itx.</summary>
    private static string? NormalizeFormFactor(string? value) => value?.ToLowerInvariant() switch
    {
        "mini-itx" or "miniitx" => "itx",
        "m-atx" or "matx" or "micro-atx" or "microatx" => "matx",
        "e-atx" or "eatx" => "eatx",
        "atx" => "atx",
        "itx" => "itx",
        _ => value?.ToLowerInvariant(),
    };

    private static string? NormalizeChipset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var s = value.ToLowerInvariant().Replace(" ", "");
        if (s.StartsWith("amd", StringComparison.Ordinal))
            s = s[3..];
        else if (s.StartsWith("intel", StringComparison.Ordinal))
            s = s[5..];

        return s.Length == 0 ? null : s;
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

    [GeneratedRegex(@"\b(\d{3,4})\s*[Ww](?:atts?)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex PsuWattage();

    [GeneratedRegex(@"(?<![A-Za-z0-9])(?:AMD|Intel\s+)?([ABXHZ]\d{3})", RegexOptions.IgnoreCase)]
    private static partial Regex Chipset();

    [GeneratedRegex(@"\b(M(?:icro)?-?ATX|Mini-?ITX|E-?ATX|ATX|ITX)\b", RegexOptions.IgnoreCase)]
    private static partial Regex FormFactor();

    [GeneratedRegex(@"(\d{2,3})\s*GB", RegexOptions.IgnoreCase)]
    private static partial Regex GpuMemoryGb();

    [GeneratedRegex(@"(\d{3})\s*[xX]\s*\d{2,3}\s*[xX]\s*\d{1,3}\s*mm", RegexOptions.IgnoreCase)]
    private static partial Regex DimensionsMm();

    [GeneratedRegex(@"(?:conectores|alimenta[çc][ãa]o|power)[^0-9]{0,20}(\d+\s*x\s*\d+\s*(?:pinos?|pin)|16\s*pinos?|8\s*pinos?|12V-?2x?6?)", RegexOptions.IgnoreCase)]
    private static partial Regex PowerConnectors();
}
