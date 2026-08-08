namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Imagem imutável do build no momento da avaliação (docs/specs.md §4.1).
/// A engine nunca toca o banco — recebe isto pronto.
/// </summary>
public sealed class BuildSnapshot
{
    public required Guid BuildId { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyList<PartSpec> Parts { get; init; }

    public PartSpec? Part(PartCategory category) =>
        Parts.FirstOrDefault(p => p.Category == category);

    public IReadOnlyList<PartSpec> PartsOf(PartCategory category) =>
        Parts.Where(p => p.Category == category).ToArray();

    /// <summary>Snapshot hipotético com uma peça trocada/inserida (filtro compatibleWith).</summary>
    public BuildSnapshot With(PartSpec part)
    {
        var parts = Parts.Where(p => p.Category != part.Category).Append(part).ToArray();
        return new BuildSnapshot { BuildId = BuildId, Slug = Slug, Parts = parts };
    }
}
