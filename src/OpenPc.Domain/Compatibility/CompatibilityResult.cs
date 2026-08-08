namespace OpenPc.Domain.Compatibility;

public enum Severity
{
    Error,
    Warning,
    Info,
}

/// <summary>
/// Resultado de uma regra. Retorno nulo = dados insuficientes para avaliar —
/// spec desconhecida nunca vira erro falso (docs/specs.md §2: risco mitigado
/// com "engine trata spec ausente como desconhecido").
/// </summary>
public sealed record CompatibilityResult(
    Severity Severity,
    string Code,
    string MessagePtBr,
    IReadOnlyList<Guid> InvolvedProductIds);

public interface ICompatibilityRule
{
    string Code { get; }
    CompatibilityResult? Evaluate(BuildSnapshot build);
}
