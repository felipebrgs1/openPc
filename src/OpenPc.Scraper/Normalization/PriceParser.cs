using System.Globalization;

namespace OpenPc.Scraper.Normalization;

/// <summary>Parse de preço em formato brasileiro ("1.599,99" → 1599.99).</summary>
public static class PriceParser
{
    public static decimal? ParseBr(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var n)
            ? n
            : null;
    }
}
