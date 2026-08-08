using System.Text.RegularExpressions;
using OpenPc.Domain.Compatibility;

namespace OpenPc.Scraper.Normalization;

/// <summary>
/// Filtro de ruído de catálogo: produtos que caem na rota/categoria da loja
/// (marketplace/cross-listing) mas não pertencem a ela.
///
/// Dois mecanismos:
/// 1. Palavras-chave por categoria (ex.: contact frames em "cpu", suportes de
///    GPU em "gpu", fonte de notebook em "psu", pasta térmica em "cooler").
/// 2. Marcadores de OUTRA categoria (cross-listing): um "Ryzen" em "memory",
///    uma "Placa Mãe" em "gpu", uma "Fonte" em "memory"... Regras com borda de
///    palavra para não pegar casos legítimos (GDDR5, 80 Plus Titanium, Cooler
///    Master, Socket AM5 em CPU, ventoinha em gabinete).
///
/// Aplicado na ingestão (descarta) e pelo comando `cleanup-noise` do scraper
/// (remove do banco o que entrou antes do filtro).
/// </summary>
public static class CategoryNoiseFilter
{
    private static readonly (string Category, string[] Patterns)[] KeywordRules =
    [
        ("cpu",
        [
            "contact frame",
            "adaptador socket",
            "adaptador de socket",
            "suporte para processador",
            "suporte de processador",
            "moldura para processador",
            "protetor de processador",
            "capa de processador",
            "tampa de processador",
            "bracket",
        ]),
        ("gpu",
        [
            "suporte para placa de video",
            "suporte de placa de video",
            "suporte placa de video",
            "suport para placa de video",
            "suport placa de video",
            "suporte gpu",
            "suporte para gpu",
            "suport gpu",
            "suporte vertical",
            "gpu support",
            "gpu bracket",
            "bracket",
            "riser",
            "placa de video vertical",
            "adaptador placa de video",
            "adaptador de video",
            "cabo para gpu",
            "para gpu",
            "cabo argb",
            "cabo rgb",
            "soundbar",
            "caixa de som",
            "espelho",
        ]),
        ("psu",
        [
            "notebook",
            "carregador",
            "nobreak",
            "bancada",
            "inversor",
            "transformador",
            "bateria",
            "power bank",
            "adaptador de energia",
        ]),
        ("memory",
        [
            "para notebook",
            "notebook",
            "sodimm",
            "laptop",
        ]),
        ("motherboard",
        [
            "notebook",
            "laptop",
            "sucata",
        ]),
        ("storage",
        [
            "monitor ",
            "pendrive",
            "pen drive",
        ]),
        ("cooler",
        [
            "pasta termica",
            "pasta de cobre",
            "pasta de solda",
            "massa",
            "adesivo",
            "graxa",
            "limpeza",
            "cabo ",
            "hub controladora",
        ]),
    ];

    /// <summary>
    /// Marcador de categoria X = título que pertence a X. Um produto na
    /// categoria Y com marcador de X (X ≠ Y) é cross-listing (ruído).
    /// Fronteiras/âncoras evitam falsos positivos: "gddr5" não é ddr5,
    /// "titanium" não é titan, "Gabinete com fonte" não é fonte, "suporte
    /// para placa de vídeo" no título de gabinete não é placa de vídeo.
    /// </summary>
    private static readonly (string Category, string Regex)[] CrossMarkers =
    [
        ("cpu", @"(?<![a-z0-9])(ryzen|athlon|pentium|celeron|threadripper|xeon)(?![a-z0-9])"),
        ("cpu", @"(?<![a-z0-9])core [iu]"),
        ("gpu", @"(?<![a-z0-9])(geforce|radeon|rtx|gtx|titan|quadro)(?![a-z0-9])"),
        ("gpu", @"(?<![a-z0-9])rx(?![a-z0-9])"),
        ("gpu", @"^placa de video"),
        ("memory", @"^ddr[45]\b"),
        ("memory", @"(?<![a-z0-9])memoria(?![a-z0-9])"),
        ("memory", @"(?<![a-z0-9])sodimm(?![a-z0-9])"),
        ("memory", @"(?<![a-z0-9])para notebook(?![a-z0-9])"),
        ("motherboard", @"^placa mae"),
        ("motherboard", @"(?<![a-z0-9])chipset(?![a-z0-9])"),
        ("psu", @"^fonte"),
        ("psu", @"80 plus"),
        ("storage", @"(?<![a-z0-9])(ssd|nvme|hdd)(?![a-z0-9])"),
        ("storage", @"disco rigido"),
        ("case", @"(?<![a-z0-9])gabinete(?![a-z0-9])"),
    ];

    private static readonly (string Category, Regex Regex)[] CompiledCrossMarkers =
        CrossMarkers.Select(m => (m.Category, new Regex(m.Regex, RegexOptions.Compiled))).ToArray();

    /// <summary>true = produto não pertence à categoria (descartar/remover).</summary>
    public static bool IsNoise(string categorySlug, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        var text = MatchKey.Normalize(title);

        foreach (var (category, patterns) in KeywordRules)
        {
            if (category != categorySlug)
                continue;
            if (patterns.Any(p => text.Contains(p, StringComparison.Ordinal)))
                return true;
        }

        // CPU fora da matriz de compatibilidade da engine (Intel < 12th, Xeon,
        // AMD não-Ryzen, mobile) não serve para montar — mesma política do M3.
        // Classifica o TÍTULO CRU (MatchKey.Normalize juntaria "i5-12400F" em
        // "i512400f" e quebraria o regex de geração). Match key não serve:
        // "Ryzen 5 Pro 5650G" não gera match key (o "Pro" quebra o padrão).
        if (categorySlug == "cpu" && CpuGeneration.Classify(title) is null)
            return true;

        foreach (var (markerCategory, regex) in CompiledCrossMarkers)
        {
            if (markerCategory == categorySlug)
                continue;
            if (regex.IsMatch(text))
                return true;
        }

        return false;
    }
}
