namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Erro §4.2: CPU não consta na matriz de suporte do chipset (compatibility.json).</summary>
public sealed class CpuChipsetUnsupportedRule(CompatibilitySeed seed) : ICompatibilityRule
{
    public string Code => "CPU_CHIPSET_UNSUPPORTED";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var cpu = build.Part(PartCategory.Cpu);
        var mobo = build.Part(PartCategory.Motherboard);
        if (cpu is null || mobo is null)
            return null;

        // Socket divergente já é bloqueado por CPU_SOCKET_MISMATCH — não acumular.
        if (SocketsDiverge(cpu, mobo))
            return null;

        var generation = CpuGeneration.Classify(cpu.Model);
        var chipset = seed.Find(mobo.Str("chipset"));
        if (generation is null || chipset is null)
            return null;

        if (chipset.FindGeneration(generation) is not null)
            return null;

        return new CompatibilityResult(Severity.Error, Code,
            $"{cpu.Name} (geração {generation}) não é suportado pelo chipset {chipset.Name} da placa-mãe.",
            [cpu.ProductId, mobo.ProductId]);
    }

    internal static bool SocketsDiverge(PartSpec cpu, PartSpec mobo)
    {
        var cpuSocket = cpu.Str("socket");
        var moboSocket = mobo.Str("socket");
        return cpuSocket is not null && moboSocket is not null
            && !string.Equals(cpuSocket, moboSocket, StringComparison.OrdinalIgnoreCase);
    }
}
