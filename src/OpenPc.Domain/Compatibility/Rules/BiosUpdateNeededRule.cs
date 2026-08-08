namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Warning §4.3: CPU suportada pelo chipset apenas com BIOS atualizada.</summary>
public sealed class BiosUpdateNeededRule(CompatibilitySeed seed) : ICompatibilityRule
{
    public string Code => "BIOS_UPDATE_NEEDED";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var cpu = build.Part(PartCategory.Cpu);
        var mobo = build.Part(PartCategory.Motherboard);
        if (cpu is null || mobo is null)
            return null;

        // Socket divergente já é erro — não acumular warning aqui.
        if (CpuChipsetUnsupportedRule.SocketsDiverge(cpu, mobo))
            return null;

        var generation = CpuGeneration.Classify(cpu.Model);
        var chipset = seed.Find(mobo.Str("chipset"));
        if (generation is null || chipset is null)
            return null;

        var support = chipset.FindGeneration(generation);
        if (support is null || support.RequiredBios is null)
            return null; // não suportada (vira erro) ou suportada nativamente

        return new CompatibilityResult(Severity.Warning, Code,
            $"{cpu.Name} exige BIOS {support.RequiredBios} no chipset {chipset.Name} — atualize a placa-mãe antes de instalar.",
            [cpu.ProductId, mobo.ProductId]);
    }
}
