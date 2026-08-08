namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Executor de regras (docs/specs.md §4.1): roda todas as regras registradas
/// contra o snapshot. Regras retornam nulo quando não avaliam.
/// </summary>
public sealed class CompatibilityEngine(IEnumerable<ICompatibilityRule> rules)
{
    private readonly IReadOnlyList<ICompatibilityRule> _rules = rules.ToArray();

    public CompatibilityEvaluation Evaluate(BuildSnapshot build) =>
        new(_rules
            .Select(r => r.Evaluate(build))
            .Where(r => r is not null)
            .Select(r => r!)
            .ToArray());
}

public sealed record CompatibilityEvaluation(IReadOnlyList<CompatibilityResult> Issues)
{
    public IReadOnlyList<CompatibilityResult> Errors =>
        Issues.Where(i => i.Severity == Severity.Error).ToArray();

    public IReadOnlyList<CompatibilityResult> Warnings =>
        Issues.Where(i => i.Severity == Severity.Warning).ToArray();

    public IReadOnlyList<CompatibilityResult> Infos =>
        Issues.Where(i => i.Severity == Severity.Info).ToArray();

    public bool HasErrors => Errors.Count > 0;
}
