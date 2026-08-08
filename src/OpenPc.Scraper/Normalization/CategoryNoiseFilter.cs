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
            "captura de video",
            "capturadora",
            // VRAM antiga = descartada: DDR3/DDR4 (GT 210, GT 710, GT 1030
            // DDR4...) e GDDR5 (GT 730/740, HD 6570, RX 580, GTX 1050 Ti...).
            // "DDR5"/"Ddr5" no título de GPU é grafia comum de GDDR5 — o
            // keyword "ddr5" pega as duas grafias. GDDR6/GDDR7 e "DDR6"
            // ("GTX 1630 4GB DDR6") não casam. Typos reais de loja para
            // GDDR3 entram: "Gdd3" (GT 610), "Gdrr3" (GT 705).
            "ddr3",
            "ddr4",
            "ddr5",
            "gdd3",
            "gdrr3",
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
            // Gerações mortas de RAM: DDR2/DDR3/DDR3L, DDR SDRAM e
            // dissipadores avulsos. DDR4/DDR5 (e PC4/PC5) ficam — o builder
            // moderno só usa essas.
            "ddr2",
            "ddr3",
            "sdram",
            "dissipador de calor memoria ram",
            // RAM para máquina específica (compat listing, não produto):
            // linhas de notebook (Aspire/Inspiron/Latitude/ThinkPad...) e de
            // desktop/servidor (OptiPlex, Precision, ThinkServer...).
            // Marcas de RAM legítimas (Samsung, Hynix, Kingston...) não estão
            // aqui — "samsung" como fabricante de módulo desktop fica.
            "aspire",
            "aspiron",
            "thinkpad",
            "inspiron",
            "latitude",
            "ideapad",
            "legion",
            "nitro",
            "envy",
            "pavilion",
            "probook",
            "elitebook",
            "zenbook",
            "vivobook",
            "macbook",
            "galaxy book",
            "vaio",
            "predator",
            "helios",
            "yoga",
            "tuf",
            "omen",
            "optiplex",
            "vostro",
            "precision",
            "xps",
            "thinkcentre",
            "thinkserver",
            "thinksystem",
            "thinkagile",
            "ideacentre",
            "workstation",
            "servidor",
            "server",
            "note",
            // Cross-listing que cai na rota de memória: gadgets, gabinetes e
            // water coolers (gabinete já cai no CrossMarker "case").
            // Variantes coladas: "Full-Tower," normaliza para "fulltower"
            // (hífen+vírgula viram nada), "Full Tower" vira "full tower".
            "smartwatch",
            "water cooler",
            "watercooler",
            "mid tower",
            "midtower",
            "full tower",
            "fulltower",
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

    /// <summary>
    /// Regras regex na PRÓPRIA categoria (as keywords usam Contains; estes
    /// padrões precisam de âncora). Texto já normalizado (hífen e barra
    /// removidos): "PC3-12800" vira "pc3 12800", "Desktop/gamers" vira
    /// "desktopgamers" — por isso o lookahead de "para" usa prefixo, não borda.
    /// </summary>
    private static readonly (string Category, Regex Regex)[] CategoryRegexRules =
    [
        ("memory", new Regex(@"\bddr\b", RegexOptions.Compiled)),           // DDR de 1ª geração (DDR 400...)
        ("memory", new Regex(@"\bpc[23]\s*\d{4,}", RegexOptions.Compiled)), // módulos DDR2/DDR3 (PC2-5300, PC3-12800)
        // "RAM para máquina X" = compat listing (Mac Pro, ProLiant, PowerEdge,
        // Alienware, Synology...). Exceções: para desktop/pc/gamers/upgrade/
        // intel/amd são marketing legítimo. Prefixo sem borda cobre as grafias
        // coladas pela normalização ("Desktop/gamers" → "desktopgamers").
        ("memory", new Regex(@"\bpara (?!desktop|gamers?|pcs?\b|notebook|laptop|computadores?|upgrade|intel|amd)", RegexOptions.Compiled)),
        // ECC = RAM de servidor (não serve para montar). "Non-ECC"/"Sem ECC"/
        // "Não-ECC" (consumo) ficam — lookbehind de exclusão.
        ("memory", new Regex(@"(?<![a-z0-9])(?<!non )(?<!sem )(?<!nao )ecc(?![a-z0-9])", RegexOptions.Compiled)),
        // Máquinas no título sem "para" (RAM de servidor/gamer direto na
        // marca): Dell PowerEdge R440, HPE Cloudline, Alienware... Com borda
        // para não pegar "Macrovip"/"Macroway" (marcas de RAM) nem "hpe" em
        // "hpeu".
        ("memory", new Regex(@"(?<![a-z0-9])(dell|poweredge|proliant|hpe|apple|synology|alienware)(?![a-z0-9])", RegexOptions.Compiled)),
    ];

    /// <summary>
    /// Whitelist de placas-mãe: só socket/chipset que suporta a matriz de
    /// CPUs do site (Intel ≥ 12ª via LGA 1700/1851 — H610..Z890; AMD AM4/AM5
    /// — A320..X870, incluindo B840). Sufixos tolerados ("b650m", "x670e",
    /// "z790a" — hífen sai na normalização). Sem nenhum desses no título =
    /// placa antiga (LGA 1155/1150/1151/1200, FM2, AM3, H61/B75/H81/H110/
    /// H310/H510), de notebook/tablet ou de máquina específica — não serve
    /// para montar. Blacklist por memória não funciona: H110/H510 antigas
    /// usam DDR4; a distinção é socket/chipset. TRX50/W790 (Threadripper/
    /// Xeon) ficam de fora: as CPUs deles já são filtradas do catálogo.
    /// </summary>
    private static readonly Regex ModernMotherboard = new(
        @"\b(1700|1851|am4|am5|strx50|str5|h610|b660|b760|z690|z790|h770|w680|b860|z890|h810"
        + @"|a320|b350|x370|b450|x470|b550|x570|a520|a620|b650|x670|b840|b850|x870)[a-z0-9]*\b",
        RegexOptions.Compiled);

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

        foreach (var (category, regex) in CategoryRegexRules)
        {
            if (category == categorySlug && regex.IsMatch(text))
                return true;
        }

        // CPU fora da matriz de compatibilidade da engine (Intel < 12th, Xeon,
        // AMD não-Ryzen, mobile) não serve para montar — mesma política do M3.
        // Classifica o TÍTULO CRU (MatchKey.Normalize juntaria "i5-12400F" em
        // "i512400f" e quebraria o regex de geração). Match key não serve:
        // "Ryzen 5 Pro 5650G" não gera match key (o "Pro" quebra o padrão).
        if (categorySlug == "cpu" && CpuGeneration.Classify(title) is null)
            return true;

        // Placas-mãe: whitelist de socket/chipset moderno (ver ModernMotherboard).
        if (categorySlug == "motherboard" && !ModernMotherboard.IsMatch(text))
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
