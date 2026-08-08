using System.Text.RegularExpressions;

namespace OpenPc.Scraper.Normalization;

/// <summary>
/// Extração do part number do fabricante (âncora primária do dedup — nenhuma
/// loja expõe EAN no front). Padrões por fabricante, validados no spike M1.
/// </summary>
public static partial class PartNumber
{
    // AMD: 100-100000926WOF, 100-100001721WOF
    [GeneratedRegex(@"\b\d{3}-\d{9}[A-Z]{2,4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex Amd();

    // Intel boxed: BX8071512400F, BX80768250K
    [GeneratedRegex(@"\bBX\d{6,9}[A-Z0-9]*\b", RegexOptions.IgnoreCase)]
    private static partial Regex IntelBoxed();

    // NVIDIA fundição (ex.: 900-1G136-2530-000) — raro no front; sem padrão confiável.
    // GPU: part number fica no slug de Pichau/Kabum quando presente.

    public static string? Extract(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var m = Amd().Match(text);
        if (!m.Success)
            m = IntelBoxed().Match(text);
        return m.Success ? Normalize(m.Value) : null;
    }

    /// <summary>Normaliza para comparação: uppercase, sem hífens/espaços.</summary>
    public static string Normalize(string partNumber) =>
        new(partNumber.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
