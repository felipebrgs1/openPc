namespace OpenPc.Domain.Compatibility;

/// <summary>
/// Estimador de consumo (docs/specs.md §4.3): TDP de CPU+GPU + overhead fixo
/// do resto do sistema (mobo, RAM, storage, fans); margem recomendada = ×1.4.
/// Quando `tdp_w` não foi extraído do scraping (ficha técnica ainda não é
/// coletada), usa o seed curado (tdp.json) como fallback por modelo.
/// `Known = false` quando há CPU/GPU no build sem TDP conhecido — o valor
/// exibido seria enganoso (só overhead).
/// </summary>
public static class WattageEstimator
{
    public const decimal SystemOverheadW = 100m;
    public const decimal MarginFactor = 1.4m;

    public static WattageEstimate Estimate(BuildSnapshot build, TdpSeed? seed = null)
    {
        var cpu = build.Part(PartCategory.Cpu);
        var gpu = build.Part(PartCategory.Gpu);

        var cpuW = cpu?.Num("tdp_w")
            ?? seed?.Find(PartCategory.Cpu, cpu?.Model, cpu?.Name);
        var gpuW = gpu?.Num("tdp_w")
            ?? seed?.Find(PartCategory.Gpu, gpu?.Model, gpu?.Name);

        var unknown = (cpu is not null && cpuW is null) || (gpu is not null && gpuW is null);
        var baseW = (cpuW ?? 0m) + (gpuW ?? 0m) + SystemOverheadW;
        return new WattageEstimate(baseW, decimal.Round(baseW * MarginFactor, 0), !unknown);
    }
}

public sealed record WattageEstimate(decimal BaseW, decimal RecommendedW, bool Known = true);
