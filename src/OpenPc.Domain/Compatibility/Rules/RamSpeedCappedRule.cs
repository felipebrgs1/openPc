namespace OpenPc.Domain.Compatibility.Rules;

/// <summary>Warning §4.3: memory.speed_mhz &gt; cpu.max_memory_speed → funciona, mas capado.</summary>
public sealed class RamSpeedCappedRule : ICompatibilityRule
{
    public string Code => "RAM_SPEED_CAPPED";

    public CompatibilityResult? Evaluate(BuildSnapshot build)
    {
        var memory = build.Part(PartCategory.Memory);
        var cpu = build.Part(PartCategory.Cpu);
        if (memory is null || cpu is null)
            return null;

        var speed = memory.Num("speed_mhz");
        var max = cpu.Num("max_memory_speed");
        if (speed is null || max is null)
            return null;

        if (speed <= max)
            return null;

        return new CompatibilityResult(Severity.Warning, Code,
            $"Memória de {speed:0} MHz opera abaixo da velocidade nominal — CPU suporta até {max:0} MHz.",
            [memory.ProductId, cpu.ProductId]);
    }
}
