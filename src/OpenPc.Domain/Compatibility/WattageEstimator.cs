namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Estimador de consumo (docs/specs.md §4.3): TDP de CPU+GPU + overhead fixo
/// do resto do sistema (mobo, RAM, storage, fans); margem recomendada = ×1.4.
/// </summary>
public static class WattageEstimator
{
    public const decimal SystemOverheadW = 100m;
    public const decimal MarginFactor = 1.4m;

    public static WattageEstimate Estimate(BuildSnapshot build)
    {
        var cpuW = build.Part(PartCategory.Cpu)?.Num("tdp_w") ?? 0m;
        var gpuW = build.Part(PartCategory.Gpu)?.Num("tdp_w") ?? 0m;
        var baseW = cpuW + gpuW + SystemOverheadW;
        return new WattageEstimate(baseW, decimal.Round(baseW * MarginFactor, 0));
    }
}

public sealed record WattageEstimate(decimal BaseW, decimal RecommendedW);
