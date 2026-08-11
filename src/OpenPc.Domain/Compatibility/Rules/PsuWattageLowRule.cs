namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Warning §4.3: psu.wattage &lt; recomendado (estimativa ×1.4 ou gpu.recommended_psu_w).</summary>
public sealed class PsuWattageLowRule(TdpSeed? seed = null) : ICompatibilityRule
{
    public string Code => "PSU_WATTAGE_LOW";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var psu = build.Part(PartCategory.Psu);
        var cpu = build.Part(PartCategory.Cpu);
        var gpu = build.Part(PartCategory.Gpu);
        if (psu is null || (cpu is null && gpu is null))
            return null; // sem processador e sem GPU não há consumo a estimar

        var wattage = psu.Num("wattage");
        if (wattage is null)
            return null;

        var estimate = WattageEstimator.Estimate(build, seed);
        if (!estimate.Known)
            return null; // TDP desconhecido — não dá para julgar a fonte sem falso alarme
        var gpuRecommended = gpu?.Num("recommended_psu_w");
        var required = gpuRecommended is not null
            ? Math.Max(estimate.RecommendedW, gpuRecommended.Value)
            : estimate.RecommendedW;

        if (wattage >= required)
            return null;

        return new CompatibilityResult(Severity.Warning, Code,
            $"Fonte de {wattage:0} W pode ser insuficiente — recomendado {required:0} W (consumo estimado {estimate.BaseW:0} W).",
            [psu.ProductId]);
    }
}
