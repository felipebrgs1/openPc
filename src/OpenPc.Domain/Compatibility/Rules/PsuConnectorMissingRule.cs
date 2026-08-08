namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: GPU exige conector ausente em psu.connectors.</summary>
public sealed class PsuConnectorMissingRule : ICompatibilityRule
{
    public string Code => "PSU_CONNECTOR_MISSING";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var gpu = build.Part(PartCategory.Gpu);
        var psu = build.Part(PartCategory.Psu);
        if (gpu is null || psu is null)
            return null;

        var required = PowerConnectorSet.Parse(gpu.Str("power_connectors"));
        var available = PowerConnectorSet.Parse(psu.Str("connectors"));
        if (required is null || available is null)
            return null;

        if (available.Value.Satisfies(required.Value))
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"Fonte não possui o conector exigido pela placa de vídeo ({required.Value}).",
            [gpu.ProductId, psu.ProductId]);
    }
}
