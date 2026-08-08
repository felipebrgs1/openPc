namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Matriz editorial socket/chipset/BIOS (docs/specs.md §4.4). Dado curado no
/// repo (Infrastructure/Seeds/compatibility.json), não vem do scraping.
/// </summary>
public sealed class CompatibilitySeed
{
    public required IReadOnlyList<ChipsetSupport> Chipsets { get; init; }

    /// <summary>
    /// Busca por nome de chipset normalizado ("B650M", "AMD B550", "x870e").
    /// Sufixos de variante de placa (m/e) são ignorados: B650M → B650.
    /// </summary>
    public ChipsetSupport? Find(string? rawChipset)
    {
        var key = Normalize(rawChipset);
        if (key is null)
            return null;

        var exact = Chipsets.FirstOrDefault(c => c.Name == key);
        if (exact is not null)
            return exact;

        if (key.Length > 4 && (key[^1] == 'm' || key[^1] == 'e'))
            return Chipsets.FirstOrDefault(c => c.Name == key[..^1]);

        return null;
    }

    private static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = new string(raw.ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch))
            .ToArray());

        if (s.StartsWith("amd", StringComparison.Ordinal))
            s = s[3..];
        else if (s.StartsWith("intel", StringComparison.Ordinal))
            s = s[5..];

        return s.Length == 0 ? null : s;
    }
}

public sealed class ChipsetSupport
{
    public required string Name { get; init; }
    public required string Socket { get; init; }
    public required IReadOnlyList<GenerationSupport> Generations { get; init; }

    public GenerationSupport? FindGeneration(string? id) =>
        Generations.FirstOrDefault(g => g.Id == id);
}

/// <summary>Suporte de uma geração de CPU num chipset. RequiredBios != null ⇒ exige BIOS atualizada.</summary>
public sealed class GenerationSupport
{
    public required string Id { get; init; }
    public string? RequiredBios { get; init; }
}
