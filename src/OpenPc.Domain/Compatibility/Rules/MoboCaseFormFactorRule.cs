namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: motherboard.form_factor ∉ case.supported_form_factors.</summary>
public sealed class MoboCaseFormFactorRule : ICompatibilityRule
{
    public string Code => "MOBO_CASE_FORM_FACTOR";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var mobo = build.Part(PartCategory.Motherboard);
        var chassis = build.Part(PartCategory.Case);
        if (mobo is null || chassis is null)
            return null;

        var formFactor = mobo.Str("form_factor");
        var supported = chassis.StrList("supported_form_factors");
        if (formFactor is null || supported.Count == 0)
            return null;

        if (supported.Any(s => string.Equals(s, formFactor, StringComparison.OrdinalIgnoreCase)))
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Formato {formFactor} da placa-mãe não é suportado pelo gabinete.",
            [mobo.ProductId, chassis.ProductId]);
    }
}
