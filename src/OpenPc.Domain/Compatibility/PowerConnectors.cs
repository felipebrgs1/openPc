using System.Text.RegularExpressions;

namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Conectores de alimentação PCIe normalizados: contagem de 8-pin (inclui
/// 6+2), 6-pin e 16-pin (12VHPWR / 12V-2x6). Fontes e GPUs declaram no
/// formato "2x8pin", "1x16 pinos", "1x12vhpwr" (docs/specs.md §3.2).
/// </summary>
public readonly record struct PowerConnectorSet(int EightPin, int SixPin, int SixteenPin)
{
    /// <summary>null = texto irreconhecível (regra não avalia — spec desconhecida).</summary>
    public static PowerConnectorSet? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var text = raw.ToLowerInvariant()
            .Replace("6+2", "8")   // 6+2 pin = conector PCIe 8-pin (antes de remover '+')
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("+", "")
            .Replace("pinos", "pin");

        var sixteen = Count(text, "16pin") + Count(text, "12v2x6") + Count(text, "12vhpwr");
        var eight = Count(text, "8pin");
        var six = Count(text, "6pin");

        if (sixteen == 0 && eight == 0 && six == 0)
            return null;

        return new PowerConnectorSet(eight, six, sixteen);
    }

    /// <summary>Esta fonte atende à exigência da GPU?</summary>
    public bool Satisfies(PowerConnectorSet required) =>
        required.SixteenPin > 0
            ? SixteenPin >= required.SixteenPin
            : required.EightPin > 0
                ? EightPin >= required.EightPin
                : required.SixPin > 0 && (EightPin >= 1 || SixPin >= 1);

    public override string ToString()
    {
        var parts = new List<string>();
        if (SixteenPin > 0)
            parts.Add($"{SixteenPin}x16pin");
        if (EightPin > 0)
            parts.Add($"{EightPin}x8pin");
        if (SixPin > 0)
            parts.Add($"{SixPin}x6pin");
        return string.Join(" + ", parts);
    }

    /// <summary>Conta "Nx&lt;token&gt;"; "&lt;token&gt;" solto (sem contagem) vale 1.</summary>
    private static int Count(string text, string token)
    {
        var total = 0;
        foreach (Match m in Regex.Matches(text, $@"(\d+)x{token}"))
            total += int.Parse(m.Groups[1].Value);

        if (total > 0)
            return total;

        return Regex.IsMatch(text, $@"(?<!\d){token}") ? 1 : 0;
    }
}
