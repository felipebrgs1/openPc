namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: cooler.radiator_mm ∉ case.radiator_support_mm.</summary>
public sealed class AioRadiatorFitRule : ICompatibilityRule
{
    public string Code => "AIO_RADIATOR_FIT";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var cooler = build.Part(PartCategory.Cooler);
        var chassis = build.Part(PartCategory.Case);
        if (cooler is null || chassis is null)
            return null;

        var radiator = cooler.Num("radiator_mm");
        var supported = chassis.NumList("radiator_support_mm");
        if (radiator is null || supported.Count == 0)
            return null;

        if (supported.Any(r => r == radiator))
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Radiador de {radiator:0} mm não é suportado pelo gabinete.",
            [cooler.ProductId, chassis.ProductId]);
    }
}
